using Xunit;
using AvaBot.API.Validators;
using AvaBot.DTO;

namespace AvaBot.Tests.API.Validators;

public class WhatsappContactInsertInfoValidatorTest
{
    private readonly WhatsappContactInsertInfoValidator _sut = new();

    [Fact]
    public void ValidContact_ShouldPass()
    {
        Assert.True(_sut.Validate(new WhatsappContactInsertInfo { Name = "Ana", Phone = "11 98765-4321" }).IsValid);
    }

    [Theory]
    [InlineData("", "11 98765-4321", "Nome e obrigatorio")]
    [InlineData("Ana", "", "Telefone e obrigatorio")]
    [InlineData("Ana", "123", "Telefone invalido. Informe DDD e numero, com ou sem codigo do pais")]
    public void InvalidContact_ShouldFail(string name, string phone, string message)
    {
        var result = _sut.Validate(new WhatsappContactInsertInfo { Name = name, Phone = phone });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == message);
    }

    [Fact]
    public void NameWith121Chars_ShouldFail()
    {
        var result = _sut.Validate(new WhatsappContactInsertInfo { Name = new string('a', 121), Phone = "11987654321" });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Nome deve ter no maximo 120 caracteres");
    }
}

public class WhatsappAccessConfigUpdateInfoValidatorTest
{
    private readonly WhatsappAccessConfigUpdateInfoValidator _sut = new();

    [Fact]
    public void NullMessages_ShouldPass()
    {
        Assert.True(_sut.Validate(new WhatsappAccessConfigUpdateInfo { Mode = 2 }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void InvalidMode_ShouldFail(int mode)
    {
        Assert.False(_sut.Validate(new WhatsappAccessConfigUpdateInfo { Mode = mode }).IsValid);
    }

    [Fact]
    public void EmptyMessage_ShouldFail()
    {
        var result = _sut.Validate(new WhatsappAccessConfigUpdateInfo { Mode = 1, OfferMessage = "   " });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.StartsWith(WhatsappAccessConfigUpdateInfoValidator.EmptyMessage));
    }

    [Fact]
    public void MessageWith501Chars_ShouldFail()
    {
        var result = _sut.Validate(new WhatsappAccessConfigUpdateInfo { Mode = 1, DeclinedMessage = new string('a', 501) });

        Assert.Contains(result.Errors, e => e.ErrorMessage.StartsWith(WhatsappAccessConfigUpdateInfoValidator.TooLongMessage));
    }
}

public class WhatsappAccessRequestApproveInfoValidatorTest
{
    private readonly WhatsappAccessRequestApproveInfoValidator _sut = new();

    [Fact]
    public void NullName_ShouldPass()
    {
        Assert.True(_sut.Validate(new WhatsappAccessRequestApproveInfo()).IsValid);
    }

    [Fact]
    public void NameWith121Chars_ShouldFail()
    {
        Assert.False(_sut.Validate(new WhatsappAccessRequestApproveInfo { Name = new string('a', 121) }).IsValid);
    }
}
