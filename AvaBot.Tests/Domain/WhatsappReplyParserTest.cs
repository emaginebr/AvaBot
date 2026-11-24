using Xunit;
using AvaBot.Domain.Models;

namespace AvaBot.Tests.Domain;

public class WhatsappReplyParserTest
{
    [Theory]
    [InlineData("sim")]
    [InlineData("Sim!")]
    [InlineData(" SIM. ")]
    [InlineData("s")]
    public void Parse_ShouldRecognizeYes(string text)
    {
        Assert.Equal(WhatsappReply.Yes, WhatsappReplyParser.Parse(text));
    }

    [Theory]
    [InlineData("não")]
    [InlineData("Nao")]
    [InlineData("N")]
    [InlineData("NÃO!!")]
    public void Parse_ShouldRecognizeNo(string text)
    {
        Assert.Equal(WhatsappReply.No, WhatsappReplyParser.Parse(text));
    }

    [Theory]
    [InlineData("sim, quero")]
    [InlineData("talvez")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_ShouldReturnNone_ForAnythingElse(string? text)
    {
        Assert.Equal(WhatsappReply.None, WhatsappReplyParser.Parse(text));
    }
}
