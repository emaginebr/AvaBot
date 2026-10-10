using AvaBot.Infra.AppServices;
using Xunit;

namespace AvaBot.Tests.Infra;

public class Pbkdf2PasswordHasherTest
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldUseThePbkdf2Sha256FormatWithIterations()
    {
        var hash = _sut.Hash("minhasenha123");

        Assert.StartsWith("pbkdf2-sha256$210000$", hash);
        Assert.Equal(4, hash.Split('$').Length);
        Assert.DoesNotContain("minhasenha123", hash);
    }

    [Fact]
    public void Hash_ShouldDifferForTheSamePassword_BecauseOfTheSalt()
    {
        var first = _sut.Hash("mesma-senha");
        var second = _sut.Hash("mesma-senha");

        Assert.NotEqual(first, second);
        Assert.True(_sut.Verify("mesma-senha", first));
        Assert.True(_sut.Verify("mesma-senha", second));
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatches()
    {
        var hash = _sut.Hash("Senha!Forte#2026");

        Assert.True(_sut.Verify("Senha!Forte#2026", hash));
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordIsWrong()
    {
        var hash = _sut.Hash("Senha!Forte#2026");

        Assert.False(_sut.Verify("senha!forte#2026", hash));
        Assert.False(_sut.Verify("", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("texto-qualquer")]
    [InlineData("pbkdf2-sha256$abc$salt$hash")]
    [InlineData("pbkdf2-sha256$210000$nao-base64!!$nao-base64!!")]
    [InlineData("bcrypt$10$salt$hash")]
    public void Verify_ShouldReturnFalse_WhenStoredHashIsMalformed(string stored)
    {
        Assert.False(_sut.Verify("qualquer", stored));
    }
}
