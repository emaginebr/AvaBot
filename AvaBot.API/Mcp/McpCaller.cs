using System.Security.Claims;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.API.Auth;

namespace AvaBot.API.Mcp;

/// <summary>
/// Identidade do chamador de uma ferramenta: o dono da chave e a chave usada (research R4).
/// O ownerUserId e passado explicitamente aos services, como o painel faz.
/// </summary>
public sealed record McpCaller(long UserId, long KeyId, string KeyName)
{
    public static McpCaller From(RequestContext<CallToolRequestParams> context)
    {
        return From(context.User);
    }

    public static McpCaller From(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            throw new McpException(McpAccessKeyAuthenticationHandler.InvalidKeyMessage);

        try
        {
            return new McpCaller(user.GetUserId(), user.GetMcpAccessKeyId(), user.GetMcpAccessKeyName());
        }
        catch (UnauthorizedAccessException)
        {
            throw new McpException(McpAccessKeyAuthenticationHandler.InvalidKeyMessage);
        }
    }
}
