using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AvaBot.Domain.Models;

namespace AvaBot.API.Auth;

/// <summary>
/// Emite o JWT HS256 do painel (research R2). Fica na API porque o pacote JWT
/// so existe aqui; as camadas de baixo nao conhecem token.
/// </summary>
public class JwtTokenIssuer
{
    public const int DefaultExpirationMinutes = 43200; // 30 dias

    private readonly SigningCredentials _credentials;
    private readonly int _expirationMinutes;

    public JwtTokenIssuer(IConfiguration configuration)
    {
        var secret = configuration["Auth:JwtSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Auth:JwtSecret is required");

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        _expirationMinutes = int.TryParse(configuration["Auth:TokenExpirationMinutes"], out var minutes) && minutes > 0
            ? minutes
            : DefaultExpirationMinutes;
    }

    public (string Token, DateTime ExpiresAt) Issue(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: _credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
