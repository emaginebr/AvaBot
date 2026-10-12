using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.API.Auth;

namespace AvaBot.Tests.API.Mcp;

/// <summary>Monta RequestContext e ClaimsPrincipal para os testes das ferramentas e filtros MCP.</summary>
public static class McpTestContext
{
    public const long UserId = 7;
    public const long KeyId = 3;

    public static ClaimsPrincipal Principal(long userId = UserId, long keyId = KeyId, string keyName = "Claude")
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "ana@exemplo.com"),
            new Claim(ClaimTypes.Name, "Ana"),
            new Claim(ClaimsPrincipalExtensions.McpKeyIdClaim, keyId.ToString()),
            new Claim(ClaimsPrincipalExtensions.McpKeyNameClaim, keyName)
        ], "McpAccessKey");

        return new ClaimsPrincipal(identity);
    }

    public static Dictionary<string, JsonElement> Arguments(params (string Key, object? Value)[] pairs)
    {
        var dict = new Dictionary<string, JsonElement>();
        foreach (var (key, value) in pairs)
            dict[key] = JsonSerializer.SerializeToElement(value);
        return dict;
    }

    public static RequestContext<CallToolRequestParams> Context(
        string toolName,
        Dictionary<string, JsonElement>? arguments = null,
        ClaimsPrincipal? user = null,
        IServiceProvider? services = null)
    {
        var server = new Mock<McpServer>().Object;
        var request = new JsonRpcRequest { Method = "tools/call" };
        var parameters = new CallToolRequestParams { Name = toolName, Arguments = arguments };

        return new RequestContext<CallToolRequestParams>(server, request, parameters)
        {
            User = user ?? Principal(),
            Services = services
        };
    }

    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Next(Func<CallToolResult> produce)
        => (_, _) => new ValueTask<CallToolResult>(produce());

    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Throwing(Exception ex)
        => (_, _) => throw ex;

    public static string Text(CallToolResult result)
        => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}

/// <summary>Logger que guarda as linhas formatadas, para conferir a auditoria sem olhar o console.</summary>
public sealed class ListLogger : ILogger
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}
