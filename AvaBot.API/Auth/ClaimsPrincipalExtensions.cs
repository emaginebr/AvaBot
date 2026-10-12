using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AvaBot.API.Auth;

public static class ClaimsPrincipalExtensions
{
    // Claims emitidas pelo McpAccessKeyAuthenticationHandler (feature 017).
    public const string McpKeyIdClaim = "mcp_key_id";
    public const string McpKeyNameClaim = "mcp_key_name";

    /// <summary>
    /// Id do usuario autenticado (claim sub do token). Um token antigo, sem sub,
    /// cai no throw e o controller responde 401 (contrato ownership.md).
    /// </summary>
    public static long GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!long.TryParse(raw, out var userId))
            throw new UnauthorizedAccessException("Token sem identificador de usuario");

        return userId;
    }

    /// <summary>Id da chave de acesso MCP que autenticou a requisicao.</summary>
    public static long GetMcpAccessKeyId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(McpKeyIdClaim);
        if (!long.TryParse(raw, out var keyId))
            throw new UnauthorizedAccessException("Chave de acesso sem identificador");

        return keyId;
    }

    /// <summary>Nome da chave de acesso MCP (vazio quando a requisicao nao veio por chave).</summary>
    public static string GetMcpAccessKeyName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(McpKeyNameClaim) ?? string.Empty;
    }
}
