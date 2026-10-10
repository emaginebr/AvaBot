using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AvaBot.API.Auth;

public static class ClaimsPrincipalExtensions
{
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
}
