using System.Text;
using FluentAssertions;
using Flurl.Http;
using Avachat.Tests.API.Support;

namespace Avachat.Tests.API.Controllers;

public class KnowledgeFileControllerTest : TestBase
{
    private async Task<long> CreateTestAgentAsync()
    {
        var response = await Api("agents")
            .PostJsonAsync(new { name = $"KF Agent {Guid.NewGuid():N}"[..20], systemPrompt = "P" });
        var body = await response.GetJsonAsync<ApiResult<AgentResponse>>();
        return body.Dados!.AgentId;
    }

    [Fact]
    public async Task GetByAgent_ShouldReturnOk_WithFileList()
    {
        var agentId = await CreateTestAgentAsync();

        var response = await Api($"files/{agentId}").GetAsync();
        response.StatusCode.Should().Be(200);

        var body = await response.GetJsonAsync<ApiResult<List<KnowledgeFileResponse>>>();
        body.Sucesso.Should().BeTrue();
        body.Dados.Should().NotBeNull();
    }

    [Fact]
    public async Task Upload_ShouldReturnCreated_WhenValidMdFile()
    {
        var agentId = await CreateTestAgentAsync();
        var content = "# Test Knowledge\n\nThis is test content for integration testing.";
        var fileBytes = Encoding.UTF8.GetBytes(content);

        var response = await Api($"files/{agentId}")
            .PostMultipartAsync(mp =>
            {
                mp.AddFile("file", new MemoryStream(fileBytes), "test-knowledge.md", "text/markdown");
            });

        response.StatusCode.Should().Be(201);

        var body = await response.GetJsonAsync<ApiResult<KnowledgeFileResponse>>();
        body.Sucesso.Should().BeTrue();
        body.Dados!.FileName.Should().Be("test-knowledge.md");
        body.Dados.FileSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenNotMdFile()
    {
        var agentId = await CreateTestAgentAsync();
        var fileBytes = Encoding.UTF8.GetBytes("not markdown");

        var action = () => Api($"files/{agentId}")
            .PostMultipartAsync(mp =>
            {
                mp.AddFile("file", new MemoryStream(fileBytes), "test.txt", "text/plain");
            });

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenFileExists()
    {
        var agentId = await CreateTestAgentAsync();
        var fileBytes = Encoding.UTF8.GetBytes("# To delete\n\nContent.");

        var uploadResponse = await Api($"files/{agentId}")
            .PostMultipartAsync(mp =>
            {
                mp.AddFile("file", new MemoryStream(fileBytes), "to-delete.md", "text/markdown");
            });
        var uploaded = await uploadResponse.GetJsonAsync<ApiResult<KnowledgeFileResponse>>();
        var fileId = uploaded.Dados!.KnowledgeFileId;

        var response = await Api($"files/{agentId}/{fileId}").DeleteAsync();
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenFileNotExists()
    {
        var agentId = await CreateTestAgentAsync();

        var action = () => Api($"files/{agentId}/999999").DeleteAsync();
        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Reprocess_ShouldReturnOk_WhenFileExists()
    {
        var agentId = await CreateTestAgentAsync();
        var fileBytes = Encoding.UTF8.GetBytes("# Reprocess\n\nContent.");

        var uploadResponse = await Api($"files/{agentId}")
            .PostMultipartAsync(mp =>
            {
                mp.AddFile("file", new MemoryStream(fileBytes), "reprocess.md", "text/markdown");
            });
        var uploaded = await uploadResponse.GetJsonAsync<ApiResult<KnowledgeFileResponse>>();
        var fileId = uploaded.Dados!.KnowledgeFileId;

        var response = await Api($"files/{agentId}/{fileId}/reprocess").PostAsync();
        response.StatusCode.Should().Be(200);
    }
}

public class KnowledgeFileResponse
{
    public long KnowledgeFileId { get; set; }
    public long? AgentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int ProcessingStatus { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
