using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Infra.AppServices;

public class AesGcmSecretProtector : ISecretProtector
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IConfiguration _configuration;

    public AesGcmSecretProtector(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Protect(string plain)
    {
        var key = GetKey();
        var plainBytes = Encoding.UTF8.GetBytes(plain);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var payload = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        cipher.CopyTo(payload, NonceSize + TagSize);

        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string cipher)
    {
        var key = GetKey();

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(cipher);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Segredo do Power BI armazena com formato invalido");
        }

        if (payload.Length < NonceSize + TagSize)
            throw new InvalidOperationException("Segredo do Power BI armazena com formato invalido");

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipherBytes = payload.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // A chave so e lida no uso: agentes sem Power BI nao podem impedir a API de subir (research R7).
    private byte[] GetKey()
    {
        var raw = _configuration["PowerBI:SecretEncryptionKey"];
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("PowerBI:SecretEncryptionKey não configurada");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("PowerBI:SecretEncryptionKey deve ser uma chave base64 de 32 bytes");
        }

        if (key.Length != KeySize)
            throw new InvalidOperationException("PowerBI:SecretEncryptionKey deve ser uma chave base64 de 32 bytes");

        return key;
    }
}
