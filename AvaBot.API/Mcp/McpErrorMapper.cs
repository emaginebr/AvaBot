using FluentValidation;
using ModelContextProtocol;
using AvaBot.Application.Services;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.API.Mcp;

/// <summary>
/// Converte as excecoes dos services nas mensagens em portugues que o assistente le (research R6).
/// Qualquer excecao fora do mapa vira a mensagem generica; o detalhe fica so no log do servidor.
/// </summary>
public static class McpErrorMapper
{
    public static string InternalErrorMessage(string toolName) =>
        $"Erro interno ao executar {toolName}. Tente de novo; se persistir, avise o usuario.";

    public static bool IsKnown(Exception ex) => ex is KeyNotFoundException
        or InvalidOperationException
        or ArgumentException
        or PowerBIApiException
        or ValidationException
        or AgentTestFailedException
        or McpException;

    public static string ToMessage(Exception ex, string toolName) => ex switch
    {
        ValidationException validation => string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
        KeyNotFoundException or InvalidOperationException or ArgumentException or PowerBIApiException
            or AgentTestFailedException or McpException => ex.Message,
        _ => InternalErrorMessage(toolName)
    };
}
