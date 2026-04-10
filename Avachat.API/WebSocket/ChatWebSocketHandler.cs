using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Avachat.Application.Services;

namespace Avachat.API.WebSocket;

public static class ChatWebSocketHandler
{
    public static void MapChatWebSocket(this WebApplication app)
    {
        app.Map("/ws/chat/{slug}", async (HttpContext context, string slug) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var agentService = context.RequestServices.GetRequiredService<AgentService>();
            var chatService = context.RequestServices.GetRequiredService<ChatService>();

            var agent = await agentService.GetBySlugAsync(slug);
            if (agent == null || agent.Status == 0)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var ws = await context.WebSockets.AcceptWebSocketAsync();
            long? sessionId = null;

            // Check if sessionId was provided via query string (created via REST endpoint)
            if (long.TryParse(context.Request.Query["sessionId"], out var providedSessionId))
            {
                sessionId = providedSessionId;
            }

            try
            {
                string? resumeToken = null;

            if (sessionId.HasValue)
                {
                    // Session already created via REST, ready to chat
                    // Fetch resume token from session
                    var existingSession = await chatService.GetSessionByIdAsync(sessionId.Value);
                    resumeToken = existingSession?.ResumeToken;
                    await SendJsonAsync(ws, new { type = "ready", sessionId = sessionId.Value, resumeToken });
                }
                else
                {
                    // Legacy flow: collect data via WebSocket
                    var fields = new List<string>();
                    if (agent.CollectName) fields.Add("name");
                    if (agent.CollectEmail) fields.Add("email");
                    if (agent.CollectPhone) fields.Add("phone");

                    if (fields.Count > 0)
                    {
                        await SendJsonAsync(ws, new { type = "collect_data", fields });
                    }
                    else
                    {
                        var session = await chatService.CreateSessionAsync(agent.AgentId, null, null, null);
                        sessionId = session.ChatSessionId;
                        resumeToken = session.ResumeToken;
                        await SendJsonAsync(ws, new { type = "ready", sessionId = sessionId.Value, resumeToken });
                    }
                }

                // Message loop
                var buffer = new byte[4096];
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<JsonElement>(messageText);
                    var msgType = message.GetProperty("type").GetString();

                    if (msgType == "identify")
                    {
                        var name = message.TryGetProperty("name", out var n) ? n.GetString() : null;
                        var email = message.TryGetProperty("email", out var e) ? e.GetString() : null;
                        var phone = message.TryGetProperty("phone", out var p) ? p.GetString() : null;

                        var session = await chatService.CreateSessionAsync(agent.AgentId, name, email, phone);
                        sessionId = session.ChatSessionId;
                        resumeToken = session.ResumeToken;
                        await SendJsonAsync(ws, new { type = "ready", sessionId = sessionId.Value, resumeToken });
                    }
                    else if (msgType == "message" && sessionId.HasValue)
                    {
                        var content = message.GetProperty("content").GetString() ?? "";

                        try
                        {
                            await foreach (var token in chatService.ProcessMessageAsync(
                                agent.AgentId, sessionId.Value, agent.ChatModel, agent.SystemPrompt, content))
                            {
                                await SendJsonAsync(ws, new { type = "chunk", content = token });
                            }
                            await SendJsonAsync(ws, new { type = "done" });
                        }
                        catch (Exception ex)
                        {
                            await SendJsonAsync(ws, new { type = "error", message = ex.Message });
                        }
                    }
                }
            }
            catch (WebSocketException)
            {
                // Connection dropped
            }
            finally
            {
                if (sessionId.HasValue)
                {
                    await chatService.EndSessionAsync(sessionId.Value);
                }

                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        });
    }

    private static async Task SendJsonAsync(System.Net.WebSockets.WebSocket ws, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
