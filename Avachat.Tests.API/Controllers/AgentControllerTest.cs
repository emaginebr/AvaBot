using FluentAssertions;
using Flurl.Http;
using Avachat.Tests.API.Support;

namespace Avachat.Tests.API.Controllers;

public class AgentControllerTest : TestBase
{
    private async Task<AgentResponse> CreateAgentAsync(string name = "Test Agent", string systemPrompt = "P")
    {
        var response = await Api("agents")
            .PostJsonAsync(new { name, systemPrompt });
        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        return body.Dados!;
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WithAutoSlug()
    {
        var response = await Api("agents")
            .PostJsonAsync(new
            {
                name = "Integration Test Agent",
                description = "Created by integration test",
                systemPrompt = "You are a test agent",
                collectName = true,
                collectEmail = false,
                collectPhone = false
            });

        response.StatusCode.Should().Be(201);

        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.AgentId.Should().BeGreaterThan(0);
        body.Dados.Slug.Should().NotBeNullOrEmpty();
        body.Dados.Slug.Should().Contain("integration-test-agent");
    }

    [Fact]
    public async Task Create_ShouldGenerateUniqueSlug_WhenDuplicateName()
    {
        var agent1 = await CreateAgentAsync("Duplicate Name Agent");
        var agent2 = await CreateAgentAsync("Duplicate Name Agent");

        agent1.Slug.Should().NotBe(agent2.Slug);
        agent2.Slug.Should().Contain("duplicate-name-agent");
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithAgentList()
    {
        await CreateAgentAsync();

        var response = await Api("agents").GetAsync();
        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<List<AgentResponse>>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnOk_WhenAgentExists()
    {
        var created = await CreateAgentAsync("Get By Slug Test");

        var response = await Api($"agents/{created.Slug}").GetAsync();
        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.Slug.Should().Be(created.Slug);
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnNotFound_WhenAgentNotExists()
    {
        var action = () => Api("agents/nonexistent-slug-xyz-99").GetAsync();
        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetChatConfig_ShouldReturnConfig_WhenAgentActive()
    {
        var response = await Api("agents")
            .PostJsonAsync(new
            {
                name = "Config Agent Test",
                systemPrompt = "P",
                collectName = true,
                collectEmail = true,
                collectPhone = false
            });
        var created = (await response.GetJsonAsync<ApiResult<AgentResponse>>()).Dados!;

        var configResponse = await Api($"agents/{created.Slug}/chat-config").GetAsync();
        configResponse.StatusCode.Should().Be(200);

        var body = await configResponse.GetJsonAsync<ApiResult<ChatConfigResponse>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.CollectName.Should().BeTrue();
        body.Dados.CollectEmail.Should().BeTrue();
        body.Dados.CollectPhone.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnOk_AndRegenerateSlug()
    {
        var created = await CreateAgentAsync("Before Update");

        var response = await Api($"agents/{created.AgentId}")
            .PutJsonAsync(new { name = "After Update", systemPrompt = "Updated" });

        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.Name.Should().Be("After Update");
        body.Dados.Slug.Should().Contain("after-update");
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenAgentNotExists()
    {
        var action = () => Api("agents/999999")
            .PutJsonAsync(new { name = "X", systemPrompt = "P" });
        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ToggleStatus_ShouldToggleAndReturn()
    {
        var created = await CreateAgentAsync("Toggle Test");

        var response = await Api($"agents/{created.AgentId}/status").PatchAsync();
        response.StatusCode.Should().Be(200);
        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        body.Dados!.Status.Should().Be(0);

        var response2 = await Api($"agents/{created.AgentId}/status").PatchAsync();
        var body2 = await response2.GetJsonAsync<ApiResult<AgentResponse>>();
        body2.Dados!.Status.Should().Be(1);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenAgentExists()
    {
        var created = await CreateAgentAsync("To Delete");

        var response = await Api($"agents/{created.AgentId}").DeleteAsync();
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenAgentNotExists()
    {
        var action = () => Api("agents/999999").DeleteAsync();
        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
    }
}

// --- Response DTOs for deserialization ---

public class ApiResult<T>
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string[] Erros { get; set; } = [];
    public T? Dados { get; set; }
}

public class AgentResponse
{
    public long AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool CollectName { get; set; }
    public bool CollectEmail { get; set; }
    public bool CollectPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ChatConfigResponse
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool CollectName { get; set; }
    public bool CollectEmail { get; set; }
    public bool CollectPhone { get; set; }
}
