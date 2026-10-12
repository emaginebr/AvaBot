using System.Security.Cryptography;
using System.Text;

namespace AvaBot.Domain.Models;

/// <summary>
/// Chave de acesso criada pelo usuario para conectar um assistente de IA ao servidor MCP (feature 017).
/// O valor em claro so existe no momento da criacao; aqui fica apenas o hash SHA-256.
/// </summary>
public class McpAccessKey
{
    public const string Prefix = "avabot_mcp_";
    private const int RandomBytes = 32;
    // Prefixo (11) + 32 bytes em base64url sem padding (43).
    public const int ExpectedLength = 54;
    public const int KeyPrefixLength = 8;

    public long McpAccessKeyId { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User? User { get; set; }

    public bool IsActive => Status == 1;

    public static string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(RandomBytes);
        var base64Url = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return Prefix + base64Url;
    }

    /// <summary>SHA-256 simples (a chave ja tem 256 bits aleatorios); deterministico para servir de indice.</summary>
    public static string ComputeHash(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key ?? string.Empty));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Evita consultar o banco para um JWT do painel ou qualquer outro valor que nao seja uma chave.</summary>
    public static bool HasExpectedFormat(string? key)
    {
        return !string.IsNullOrEmpty(key)
            && key.Length == ExpectedLength
            && key.StartsWith(Prefix, StringComparison.Ordinal);
    }

    /// <summary>Os primeiros caracteres depois do prefixo, so para o usuario reconhecer a chave na lista.</summary>
    public static string ExtractPrefix(string key)
    {
        var body = key.Length > Prefix.Length ? key[Prefix.Length..] : string.Empty;
        return body.Length > KeyPrefixLength ? body[..KeyPrefixLength] : body;
    }
}
