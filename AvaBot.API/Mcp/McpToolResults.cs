using System.Text.Json;
using ModelContextProtocol.Protocol;
using AvaBot.Application.Services;

namespace AvaBot.API.Mcp;

/// <summary>
/// Monta o CallToolResult das ferramentas: sucesso com o DTO em texto e em structuredContent;
/// erro com a mensagem em portugues (isError = true). As ferramentas devolvem CallToolResult
/// diretamente para que a mensagem chegue ao assistente sem o prefixo generico do SDK.
/// </summary>
public static class McpToolResults
{
    public static CallToolResult Ok<T>(T dto)
    {
        var json = JsonSerializer.Serialize(dto, McpJson.Options);
        return new CallToolResult
        {
            IsError = false,
            Content = [new TextContentBlock { Text = json }],
            StructuredContent = JsonSerializer.SerializeToElement(dto, McpJson.Options)
        };
    }

    public static CallToolResult Error(string message)
    {
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = message }]
        };
    }

    /// <summary>
    /// Executa a ferramenta e converte excecoes em isError com a mensagem do McpErrorMapper.
    /// A falha do modelo na pergunta de teste leva junto o resultado parcial (rodadas concluidas).
    /// </summary>
    public static async Task<CallToolResult> RunAsync<T>(ILogger logger, string toolName, Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (AgentTestFailedException ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock { Text = ex.Message },
                    new TextContentBlock { Text = JsonSerializer.Serialize(ex.PartialResult, McpJson.Options) }
                ]
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!McpErrorMapper.IsKnown(ex))
                logger.LogError(ex, "Erro inesperado na ferramenta MCP {Tool}", toolName);

            return Error(McpErrorMapper.ToMessage(ex, toolName));
        }
    }
}
