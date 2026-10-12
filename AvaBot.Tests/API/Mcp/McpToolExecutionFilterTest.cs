using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Xunit;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using AvaBot.API.Mcp;
using AvaBot.Application.Services;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Tests.API.Mcp;

public class McpToolExecutionFilterTest
{
    private readonly ListLogger _logger = new();

    private async Task<CallToolResult> RunWith(Exception ex, string tool = "obter_agente")
    {
        var handler = McpToolExecutionFilter.Create(_logger)(McpTestContext.Throwing(ex));
        return await handler(McpTestContext.Context(tool, McpTestContext.Arguments(("agentId", 12))), CancellationToken.None);
    }

    [Fact]
    public async Task ShouldMapKeyNotFound_ToIsErrorWithTheMessage()
    {
        var result = await RunWith(new KeyNotFoundException("Agente nao encontrado"));

        Assert.True(result.IsError);
        Assert.Equal("Agente nao encontrado", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ShouldMapInvalidOperationArgumentAndPowerBIExceptions_ToTheirMessages()
    {
        Assert.Equal("Configure as credenciais", McpTestContext.Text(await RunWith(new InvalidOperationException("Configure as credenciais"))));
        Assert.Equal("Status invalido", McpTestContext.Text(await RunWith(new ArgumentException("Status invalido"))));
        Assert.Equal("Dataset sem permissao", McpTestContext.Text(await RunWith(new PowerBIApiException(403, "Forbidden", "Dataset sem permissao"))));
        Assert.Equal("Chave de acesso invalida ou revogada", McpTestContext.Text(await RunWith(new McpException("Chave de acesso invalida ou revogada"))));
    }

    [Fact]
    public async Task ShouldJoinValidationMessages_WithSemicolon()
    {
        var ex = new ValidationException([
            new ValidationFailure("Name", "Nome e obrigatorio"),
            new ValidationFailure("SystemPrompt", "Prompt de sistema e obrigatorio")
        ]);

        var result = await RunWith(ex, "criar_agente");

        Assert.True(result.IsError);
        Assert.Equal("Nome e obrigatorio; Prompt de sistema e obrigatorio", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ShouldHideTheDetail_OfUnknownExceptions()
    {
        var result = await RunWith(new NullReferenceException("detalhe interno com stack"), "listar_agentes");

        Assert.True(result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Equal("Erro interno ao executar listar_agentes. Tente de novo; se persistir, avise o usuario.", text);
        Assert.DoesNotContain("detalhe interno", text);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ShouldPassThroughTheResultOfNext_OnSuccess()
    {
        var expected = McpToolResults.Ok(new McpMessageInfo { Message = "ok" });
        var handler = McpToolExecutionFilter.Create(_logger)(McpTestContext.Next(() => expected));

        var result = await handler(McpTestContext.Context("remover_documento"), CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ShouldWriteTheAuditLine_WithoutTheArgumentValues()
    {
        var handler = McpToolExecutionFilter.Create(_logger)(McpTestContext.Next(() => McpToolResults.Ok(new McpMessageInfo { Message = "ok" })));
        var context = McpTestContext.Context("atualizar_agente",
            McpTestContext.Arguments(("agentId", 12), ("systemPrompt", "PROMPT-SECRETO-DO-USUARIO")));

        await handler(context, CancellationToken.None);

        var audit = Assert.Single(_logger.Entries, e => e.Message.StartsWith("MCP tool="));
        Assert.Equal(LogLevel.Information, audit.Level);
        Assert.Contains("tool=atualizar_agente", audit.Message);
        Assert.Contains("userId=7", audit.Message);
        Assert.Contains("mcpKeyId=3", audit.Message);
        Assert.Contains("agentId=12", audit.Message);
        Assert.Contains("sucesso=True", audit.Message);
        Assert.Contains("durationMs=", audit.Message);
        Assert.DoesNotContain("PROMPT-SECRETO-DO-USUARIO", audit.Message);
    }

    [Fact]
    public async Task ShouldLogSucessoFalse_WhenTheToolFails()
    {
        await RunWith(new KeyNotFoundException("Agente nao encontrado"));

        var audit = Assert.Single(_logger.Entries, e => e.Message.StartsWith("MCP tool="));
        Assert.Contains("sucesso=False", audit.Message);
    }
}
