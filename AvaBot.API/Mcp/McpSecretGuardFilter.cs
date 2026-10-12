using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AvaBot.API.Mcp;

/// <summary>
/// Guarda de segredos (FR-010, research R5): qualquer chamada cujos argumentos tragam um campo de
/// credencial e recusada por inteiro, antes de a ferramenta rodar, para que nada seja gravado.
/// </summary>
public static class McpSecretGuardFilter
{
    public static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "openAIApiKey", "removeOpenAIApiKey", "apiKey",
        "telegramBotToken", "telegramWebhookSecret", "whatsappToken",
        "tenantId", "directoryId", "clientId", "appId", "clientSecret",
        "secret", "token", "password", "senha"
    };

    public static string RefusalMessage(string fieldName) =>
        "Credenciais (chave de API do modelo, Directory ID, App ID e segredo do Power BI, tokens do Telegram e do WhatsApp) " +
        "sao informadas apenas pelo usuario no painel, em Agentes > (agente) > Configuracoes, Power BI ou Telegram. " +
        $"Nenhum campo desta chamada foi gravado. Remova o campo '{fieldName}' e repita a operacao.";

    public static string? FindForbiddenKey(IDictionary<string, System.Text.Json.JsonElement>? arguments)
    {
        if (arguments == null) return null;

        foreach (var key in arguments.Keys)
        {
            if (ForbiddenKeys.Contains(key))
                return key;
        }

        return null;
    }

    public static McpRequestFilter<CallToolRequestParams, CallToolResult> Create()
    {
        return next => async (context, cancellationToken) =>
        {
            var forbidden = FindForbiddenKey(context.Params?.Arguments);
            if (forbidden != null)
                return McpToolResults.Error(RefusalMessage(forbidden));

            return await next(context, cancellationToken);
        };
    }
}
