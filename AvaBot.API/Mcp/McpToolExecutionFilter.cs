using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.API.Auth;

namespace AvaBot.API.Mcp;

/// <summary>
/// Envolve cada tools/call: cronometra, converte excecoes que escaparem da ferramenta em isError com
/// mensagem em portugues e escreve a linha de auditoria (FR-019). Nunca loga o conteudo dos argumentos.
/// </summary>
public static class McpToolExecutionFilter
{
    public const string AuditCategory = "AvaBot.API.Mcp.Audit";

    public static McpRequestFilter<CallToolRequestParams, CallToolResult> Create(ILogger? fixedLogger = null)
    {
        return next => async (context, cancellationToken) =>
        {
            var toolName = context.Params?.Name ?? "?";
            var logger = fixedLogger ?? ResolveLogger(context);
            var stopwatch = Stopwatch.StartNew();
            CallToolResult? result = null;

            try
            {
                result = await next(context, cancellationToken);
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (!McpErrorMapper.IsKnown(ex))
                    logger.LogError(ex, "Erro inesperado ao executar a ferramenta MCP {Tool}", toolName);

                result = McpToolResults.Error(McpErrorMapper.ToMessage(ex, toolName));
                return result;
            }
            finally
            {
                stopwatch.Stop();
                WriteAudit(logger, context, toolName, result?.IsError != true, stopwatch.ElapsedMilliseconds);
            }
        };
    }

    private static ILogger ResolveLogger(RequestContext<CallToolRequestParams> context)
    {
        var factory = context.Services?.GetService<ILoggerFactory>();
        return factory?.CreateLogger(AuditCategory) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    private static void WriteAudit(ILogger logger, RequestContext<CallToolRequestParams> context, string toolName, bool success, long durationMs)
    {
        var level = IsReadOnly(context) ? LogLevel.Debug : LogLevel.Information;
        if (!logger.IsEnabled(level)) return;

        var user = context.User;
        var userId = user != null && long.TryParse(user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var u) ? u.ToString() : "-";
        var keyId = user?.FindFirst(ClaimsPrincipalExtensions.McpKeyIdClaim)?.Value ?? "-";
        var agentId = ReadAgentId(context.Params?.Arguments);

        logger.Log(level, "MCP tool={Tool} userId={UserId} mcpKeyId={KeyId} agentId={AgentId} sucesso={Success} durationMs={Duration}",
            toolName, userId, keyId, agentId, success, durationMs);
    }

    private static bool IsReadOnly(RequestContext<CallToolRequestParams> context)
    {
        return context.MatchedPrimitive is McpServerTool tool && tool.ProtocolTool.Annotations?.ReadOnlyHint == true;
    }

    private static string ReadAgentId(IDictionary<string, JsonElement>? arguments)
    {
        if (arguments == null || !arguments.TryGetValue("agentId", out var element))
            return "-";

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.String => element.GetString() ?? "-",
            _ => "-"
        };
    }
}
