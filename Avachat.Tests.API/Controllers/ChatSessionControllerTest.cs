using FluentAssertions;
using Flurl.Http;
using Avachat.Tests.API.Support;

namespace Avachat.Tests.API.Controllers;

public class ChatSessionControllerTest : TestBase
{
    private async Task<AgentResponse> CreateTestAgentAsync(bool collectName = false)
    {
        var response = await Api("agents")
            .PostJsonAsync(new
            {
                name = $"Chat Agent {Guid.NewGuid():N}"[..20],
                systemPrompt = "You are a test assistant",
                collectName,
                collectEmail = false,
                collectPhone = false
            });
        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        return body.Dados!;
    }

    [Fact]
    public async Task GetSessions_ShouldReturnOk_WithPaginatedResult()
    {
        var agent = await CreateTestAgentAsync();

        var response = await Api($"sessions/agents/{agent.AgentId}").GetAsync();
        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<PaginatedResponse<ChatSessionResponse>>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.Items.Should().NotBeNull();
        body.Dados.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetSessions_ShouldRespectPagination()
    {
        var agent = await CreateTestAgentAsync();

        var response = await Api($"sessions/agents/{agent.AgentId}")
            .SetQueryParams(new { page = 2, maxPage = 5 })
            .GetAsync();

        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<PaginatedResponse<ChatSessionResponse>>>();
        body.Dados!.Page.Should().Be(2);
        body.Dados.MaxPage.Should().Be(5);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WithEmptyList_WhenNoSession()
    {
        var response = await Api("sessions/999999/messages").GetAsync();
        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<PaginatedResponse<ChatMessageResponse>>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.Items.Should().BeEmpty();
    }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int MaxPage { get; set; }
}

public class ChatSessionResponse
{
    public long ChatSessionId { get; set; }
    public long? AgentId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatMessageResponse
{
    public long ChatMessageId { get; set; }
    public long? ChatSessionId { get; set; }
    public int SenderType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
