using FluentAssertions;
using Flurl.Http;
using AvaBot.Tests.API.Support;

namespace AvaBot.Tests.API.Controllers;

/// <summary>Chaves de acesso MCP pelo painel (feature 017, US1): criar, listar, revogar e os 401 de cada esquema.</summary>
public class McpAccessKeyControllerTest : TestBase
{
    private readonly List<long> _createdKeyIds = new();

    private async Task<McpKeyCreatedResponse> CreateKeyAsync(string name = "Teste de API")
    {
        var response = await Api("auth/me/mcp-keys").PostJsonAsync(new { name });
        response.StatusCode.Should().Be(201);
        var body = await response.GetJsonAsync<ApiResult<McpKeyCreatedResponse>>();
        body.Sucesso.Should().BeTrue();
        _createdKeyIds.Add(body.Dados!.AccessKey.McpAccessKeyId);
        return body.Dados;
    }

    public override async Task DisposeAsync()
    {
        foreach (var id in _createdKeyIds)
        {
            try { await Api($"auth/me/mcp-keys/{id}").DeleteAsync(); } catch { /* ja revogada */ }
        }
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnThePlainKeyOnce_WithPrefixAndNeverUsed()
    {
        var created = await CreateKeyAsync("Claude do notebook");

        created.Key.Should().StartWith("avabot_mcp_");
        created.Key.Should().HaveLength(54);
        created.AccessKey.Name.Should().Be("Claude do notebook");
        created.AccessKey.KeyPrefix.Should().Be(created.Key.Substring(11, 8));
        created.AccessKey.Status.Should().Be(1);
        created.AccessKey.LastUsedAt.Should().BeNull();
        created.AccessKey.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var action = () => Api("auth/me/mcp-keys").PostJsonAsync(new { name = "  " });

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(400);
        var body = await ex.Which.GetResponseJsonAsync<ApiResult<object>>();
        body.Mensagem.Should().Be("Nome e obrigatorio");
    }

    [Fact]
    public async Task List_ShouldContainTheCreatedKey_WithoutTheHashOrTheValue()
    {
        var created = await CreateKeyAsync("Para listar");

        var response = await Api("auth/me/mcp-keys").GetAsync();
        response.StatusCode.Should().Be(200);
        var raw = await response.GetStringAsync();
        var body = await response.GetJsonAsync<ApiResult<List<McpKeyResponse>>>();

        var item = body.Dados!.Single(k => k.McpAccessKeyId == created.AccessKey.McpAccessKeyId);
        item.Name.Should().Be("Para listar");
        item.LastUsedAt.Should().BeNull();
        raw.Should().NotContain(created.Key);
        raw.Should().NotContain("keyHash");
    }

    [Fact]
    public async Task Revoke_ShouldSetStatusZero_AndBeIdempotent()
    {
        var created = await CreateKeyAsync("Para revogar");

        var response = await Api($"auth/me/mcp-keys/{created.AccessKey.McpAccessKeyId}").DeleteAsync();
        response.StatusCode.Should().Be(200);
        var body = await response.GetJsonAsync<ApiResult<McpKeyResponse>>();
        body.Mensagem.Should().Be("Chave revogada");
        body.Dados!.Status.Should().Be(0);
        body.Dados.RevokedAt.Should().NotBeNull();

        var again = await Api($"auth/me/mcp-keys/{created.AccessKey.McpAccessKeyId}").DeleteAsync();
        again.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Revoke_ShouldReturnNotFound_WhenTheKeyDoesNotExist()
    {
        var action = () => Api("auth/me/mcp-keys/999999999").DeleteAsync();

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
        var body = await ex.Which.GetResponseJsonAsync<ApiResult<object>>();
        body.Mensagem.Should().Be("Chave nao encontrada");
    }

    [Fact]
    public async Task List_ShouldReturnUnauthorized_WithoutJwt()
    {
        using var anonymous = new FlurlClient(Settings.BaseUrl);
        var action = () => anonymous.Request("auth/me/mcp-keys").GetAsync();

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task PanelRoutes_ShouldRejectTheMcpKey_AsBearer()
    {
        // Os esquemas nao se misturam: a chave MCP nao autentica o painel (contrato mcp-server.md).
        var created = await CreateKeyAsync("Chave no painel");
        using var withKey = new FlurlClient(Settings.BaseUrl).WithHeader("Authorization", $"Bearer {created.Key}");

        var action = () => withKey.Request("auth/me").GetAsync();

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(401);
    }
}

public class McpKeyResponse
{
    public long McpAccessKeyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class McpKeyCreatedResponse
{
    public string Key { get; set; } = string.Empty;
    public McpKeyResponse AccessKey { get; set; } = new();
}
