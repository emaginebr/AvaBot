using System.Security.Cryptography;
using AvaBot.Infra.AppServices;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AvaBot.Tests.Infra;

public class AesGcmSecretProtectorTest
{
    private static readonly string ValidKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private static AesGcmSecretProtector CreateProtector(string? key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PowerBI:SecretEncryptionKey"] = key
            })
            .Build();

        return new AesGcmSecretProtector(configuration);
    }

    [Fact]
    public void Protect_Unprotect_ShouldReturnTheSameSecret()
    {
        // Arrange
        var protector = CreateProtector(ValidKey);
        const string secret = "S3cr3t-Value~xyz";

        // Act
        var cipher = protector.Protect(secret);

        // Assert
        Assert.NotEqual(secret, cipher);
        Assert.Equal(secret, protector.Unprotect(cipher));
    }

    [Fact]
    public void Protect_ShouldUseARandomNonce_SoTheSameSecretCiphersDifferently()
    {
        // Arrange
        var protector = CreateProtector(ValidKey);

        // Act
        var first = protector.Protect("mesmo-segredo");
        var second = protector.Protect("mesmo-segredo");

        // Assert
        Assert.NotEqual(first, second);
        Assert.Equal("mesmo-segredo", protector.Unprotect(first));
        Assert.Equal("mesmo-segredo", protector.Unprotect(second));
    }

    [Fact]
    public void Protect_ShouldThrow_WhenKeyIsNotConfigured()
    {
        // Arrange
        var protector = CreateProtector(null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => protector.Protect("segredo"));
        Assert.Contains("PowerBI:SecretEncryptionKey", exception.Message);
    }

    [Fact]
    public void Protect_ShouldThrow_WhenKeyIsNotThirtyTwoBytes()
    {
        // Arrange
        var shortKey = Convert.ToBase64String(new byte[16]);
        var protector = CreateProtector(shortKey);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => protector.Protect("segredo"));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Protect_ShouldThrow_WhenKeyIsNotBase64()
    {
        // Arrange
        var protector = CreateProtector("nao-e-base64!!!");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => protector.Protect("segredo"));
    }

    [Fact]
    public void Unprotect_ShouldThrow_WhenCiphertextWasTampered()
    {
        // Arrange
        var protector = CreateProtector(ValidKey);
        var payload = Convert.FromBase64String(protector.Protect("segredo-original"));
        payload[^1] ^= 0xFF;

        // Act & Assert
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(Convert.ToBase64String(payload)));
    }

    [Fact]
    public void Unprotect_ShouldThrow_WhenCiphertextIsNotBase64()
    {
        // Arrange
        var protector = CreateProtector(ValidKey);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => protector.Unprotect("texto-planosem-base64"));
    }

    [Fact]
    public void Unprotect_ShouldThrow_WhenCiphertextIsTruncated()
    {
        // Arrange
        var protector = CreateProtector(ValidKey);
        var tooShort = Convert.ToBase64String(new byte[10]);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => protector.Unprotect(tooShort));
    }
}
