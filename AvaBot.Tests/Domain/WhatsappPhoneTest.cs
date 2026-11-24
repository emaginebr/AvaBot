using Xunit;
using AvaBot.Domain.Models;

namespace AvaBot.Tests.Domain;

public class WhatsappPhoneTest
{
    [Theory]
    [InlineData("+55 (11) 98765-4321")]
    [InlineData("11 98765-4321")]
    [InlineData("5511987654321")]
    [InlineData("0055 11 98765 4321")]
    public void Normalize_ShouldProduceTheSameValue_ForDifferentFormats(string input)
    {
        Assert.Equal("5511987654321", WhatsappPhone.Normalize(input));
    }

    [Fact]
    public void Normalize_ShouldPrefixBrazil_ForLandlineWithoutCountryCode()
    {
        Assert.Equal("551133334444", WhatsappPhone.Normalize("(11) 3333-4444"));
    }

    [Fact]
    public void Normalize_ShouldNotPrefixBrazil_WhenNumberHasPlus()
    {
        Assert.Equal("14155550100", WhatsappPhone.Normalize("+1 415 555 0100"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("+1234567890123456")]
    public void Normalize_ShouldReturnNull_ForInvalidValues(string? input)
    {
        Assert.Null(WhatsappPhone.Normalize(input));
    }

    [Fact]
    public void MatchKey_ShouldIgnoreTheBrazilianNinthDigit()
    {
        Assert.Equal("551187654321", WhatsappPhone.MatchKey("5511987654321"));
        Assert.Equal(WhatsappPhone.MatchKey("5511987654321"), WhatsappPhone.MatchKey("551187654321"));
    }

    [Fact]
    public void MatchKey_ShouldKeepOtherNumbers()
    {
        Assert.Equal("14155550100", WhatsappPhone.MatchKey("14155550100"));
        Assert.Equal("5511387654321", WhatsappPhone.MatchKey("5511387654321"));
        Assert.Null(WhatsappPhone.MatchKey(null));
    }

    [Fact]
    public void FromJid_ShouldExtractDigits_OnlyForCUs()
    {
        Assert.Equal("5511987654321", WhatsappPhone.FromJid("5511987654321@c.us"));
        Assert.Null(WhatsappPhone.FromJid("123456789@lid"));
        Assert.Null(WhatsappPhone.FromJid("123-456@g.us"));
        Assert.Null(WhatsappPhone.FromJid(""));
        Assert.Null(WhatsappPhone.FromJid(null));
    }
}
