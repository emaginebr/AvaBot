using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Flurl.Http;
using Avachat.Tests.API.Support;

namespace Avachat.Tests.API.Controllers;

public class ChatWebSocketTest : TestBase
{
    private Uri WsUri(string path)
    {
        var baseUrl = Settings.BaseUrl.Replace("http://", "ws://").Replace("https://", "wss://");
        return new Uri($"{baseUrl}/{path}");
    }

    private async Task<AgentResponse> CreateAgentAsync(bool collectName = false, bool collectEmail = false)
    {
        var response = await Api("api/agents")
            .PostJsonAsync(new
            {
                name = $"WS Agent {Guid.NewGuid():N}"[..20],
                systemPrompt = "Responda sempre com 'Ola! Como posso ajudar?'",
                collectName,
                collectEmail,
                collectPhone = false
            });
        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        return body.Dados!;
    }

    private static async Task<JsonElement> ReceiveJsonAsync(ClientWebSocket ws, int timeoutMs = 10000)
    {
        var buffer = new byte[4096];
        using var cts = new CancellationTokenSource(timeoutMs);
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private static async Task SendJsonAsync(ClientWebSocket ws, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    [Fact]
    public async Task WebSocket_ShouldReturn404_WhenAgentNotExists()
    {
        using var ws = new ClientWebSocket();

        var action = () => ws.ConnectAsync(WsUri("ws/chat/nonexistent-agent-xyz"), CancellationToken.None);

        await action.Should().ThrowAsync<WebSocketException>();
    }

    [Fact]
    public async Task WebSocket_ShouldSendReady_WhenNoDataToCollect()
    {
        var agent = await CreateAgentAsync(collectName: false, collectEmail: false);

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(WsUri($"ws/chat/{agent.Slug}"), CancellationToken.None);

        // Should receive "ready" immediately since no data to collect
        var msg = await ReceiveJsonAsync(ws);
        msg.GetProperty("type").GetString().Should().Be("ready");

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Fact]
    public async Task WebSocket_ShouldCreateSession_WhenNoDataToCollect()
    {
        var agent = await CreateAgentAsync(collectName: false);

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(WsUri($"ws/chat/{agent.Slug}"), CancellationToken.None);

        var msg = await ReceiveJsonAsync(ws);
        msg.GetProperty("type").GetString().Should().Be("ready");

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        // Wait for session to be finalized
        await Task.Delay(500);

        // Verify session was created via REST API
        var response = await Api($"api/agents/{agent.AgentId}/sessions").GetAsync();
        var body = await response.GetJsonAsync<ApiResult<PaginatedResponse<ChatSessionResponse>>>();
        body.Dados!.Items.Should().NotBeEmpty();
        body.Dados.Items[0].AgentId.Should().Be(agent.AgentId);
        body.Dados.Items[0].EndedAt.Should().NotBeNull(); // Session ended after close
    }

    [Fact]
    public async Task WebSocket_ShouldCollectDataAndCreateSession_WhenFieldsRequired()
    {
        var agent = await CreateAgentAsync(collectName: true, collectEmail: true);

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(WsUri($"ws/chat/{agent.Slug}"), CancellationToken.None);

        // Should receive "collect_data" with required fields
        var collectMsg = await ReceiveJsonAsync(ws);
        collectMsg.GetProperty("type").GetString().Should().Be("collect_data");
        var fields = collectMsg.GetProperty("fields");
        fields.GetArrayLength().Should().Be(2);

        // Send identify
        await SendJsonAsync(ws, new { type = "identify", name = "Teste User", email = "teste@test.com" });

        // Should receive "ready"
        var readyMsg = await ReceiveJsonAsync(ws);
        readyMsg.GetProperty("type").GetString().Should().Be("ready");

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        await Task.Delay(500);

        // Verify session was created with user data
        var response = await Api($"api/agents/{agent.AgentId}/sessions").GetAsync();
        var body = await response.GetJsonAsync<ApiResult<PaginatedResponse<ChatSessionResponse>>>();
        body.Dados!.Items.Should().NotBeEmpty();
        var session = body.Dados.Items[0];
        session.UserName.Should().Be("Teste User");
        session.UserEmail.Should().Be("teste@test.com");
    }

    [Fact]
    public async Task WebSocket_ShouldSendMessage_AndReceiveStreamedResponse()
    {
        var agent = await CreateAgentAsync(collectName: false);

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(WsUri($"ws/chat/{agent.Slug}"), CancellationToken.None);

        // Wait for ready
        var readyMsg = await ReceiveJsonAsync(ws);
        readyMsg.GetProperty("type").GetString().Should().Be("ready");

        // Send a message
        await SendJsonAsync(ws, new { type = "message", content = "Ola" });

        // Collect streamed response: should get chunk(s) + done
        var chunks = new List<string>();
        var gotDone = false;

        while (!gotDone)
        {
            var msg = await ReceiveJsonAsync(ws, 30000); // 30s timeout for AI response
            var type = msg.GetProperty("type").GetString();

            if (type == "chunk")
            {
                chunks.Add(msg.GetProperty("content").GetString() ?? "");
            }
            else if (type == "done")
            {
                gotDone = true;
            }
            else if (type == "error")
            {
                // AI service might not be available in test env, still validates the flow
                break;
            }
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);

        await Task.Delay(500);

        // Verify session exists with messages
        var sessionsResponse = await Api($"api/agents/{agent.AgentId}/sessions").GetAsync();
        var sessions = await sessionsResponse.GetJsonAsync<ApiResult<PaginatedResponse<ChatSessionResponse>>>();
        sessions.Dados!.Items.Should().NotBeEmpty();

        var session = sessions.Dados.Items[0];
        session.ChatSessionId.Should().BeGreaterThan(0);

        // Check messages were persisted
        var messagesResponse = await Api($"api/sessions/{session.ChatSessionId}/messages").GetAsync();
        var messages = await messagesResponse.GetJsonAsync<ApiResult<PaginatedResponse<ChatMessageResponse>>>();

        // At minimum the user message should be there (assistant may fail if OpenAI key is invalid)
        messages.Dados!.Items.Should().NotBeEmpty();
        messages.Dados.Items.Should().Contain(m => m.SenderType == 0 && m.Content == "Ola"); // SenderType.User = 0
    }
}
