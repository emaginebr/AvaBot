using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using AvaBot.Application.Services;

namespace AvaBot.API.Auth;

/// <summary>
/// Esquema "McpAccessKey" (feature 017): autentica o endpoint /mcp pela chave de acesso
/// (Authorization: Bearer avabot_mcp_...). Toda falha responde o mesmo 401, sem dizer o motivo.
/// </summary>
public class McpAccessKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "McpAccessKey";
    public const string PolicyName = "McpAccessKeyPolicy";
    public const string InvalidKeyMessage = "Chave de acesso invalida ou revogada";

    private readonly McpAccessKeyService _keyService;

    public McpAccessKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        McpAccessKeyService keyService)
        : base(options, logger, encoder)
    {
        _keyService = keyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var plainKey = header["Bearer ".Length..].Trim();
        var key = await _keyService.AuthenticateAsync(plainKey);
        if (key?.User == null)
            return AuthenticateResult.Fail(InvalidKeyMessage);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, key.UserId.ToString()),
            new("sub", key.UserId.ToString()),
            new(ClaimTypes.Email, key.User.Email),
            new("email", key.User.Email),
            new(ClaimTypes.Name, key.User.Name),
            new("name", key.User.Name),
            new(ClaimsPrincipalExtensions.McpKeyIdClaim, key.McpAccessKeyId.ToString()),
            new(ClaimsPrincipalExtensions.McpKeyNameClaim, key.Name)
        };

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new { sucesso = false, mensagem = InvalidKeyMessage });
        await Response.WriteAsync(body);
    }
}
