using System.Text.Json;
using Xunit;
using AvaBot.Application.Services;

namespace AvaBot.Tests.Application.Services;

public class WhatsappSenderTest
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void PrivateCUs_ShouldUseFrom_AndExtractThePhone()
    {
        var sender = WhatsappSender.FromPayload(Parse("""{"from":"5511987654321@c.us","isGroupMsg":false,"sender":{"pushname":"Maria"}}"""));

        Assert.Equal("5511987654321@c.us", sender.WhatsappId);
        Assert.Equal("5511987654321", sender.Phone);
        Assert.Equal("Maria", sender.ProfileName);
        Assert.False(sender.IsGroup);
        Assert.False(sender.IsLid);
    }

    [Fact]
    public void PrivateLid_ShouldHaveNoPhone()
    {
        var sender = WhatsappSender.FromPayload(Parse("""{"from":"998877@lid","isGroupMsg":false}"""));

        Assert.Equal("998877@lid", sender.WhatsappId);
        Assert.Null(sender.Phone);
        Assert.True(sender.IsLid);
    }

    [Fact]
    public void Group_ShouldUseTheAuthor_NotTheGroup()
    {
        var sender = WhatsappSender.FromPayload(Parse("""{"from":"120363@g.us","author":"5511987654321@c.us","isGroupMsg":true}"""));

        Assert.Equal("5511987654321@c.us", sender.WhatsappId);
        Assert.Equal("5511987654321", sender.Phone);
        Assert.True(sender.IsGroup);
    }

    [Fact]
    public void GroupWithoutAuthor_ShouldHaveEmptyJid()
    {
        var sender = WhatsappSender.FromPayload(Parse("""{"from":"120363@g.us","isGroupMsg":true}"""));

        Assert.Equal(string.Empty, sender.WhatsappId);
        Assert.Null(sender.Phone);
    }

    [Theory]
    [InlineData("""{"from":"1@c.us","sender":{"pushname":"  Ana "},"notifyName":"Outro"}""", "Ana")]
    [InlineData("""{"from":"1@c.us","sender":{"pushname":""},"notifyName":"Bia"}""", "Bia")]
    [InlineData("""{"from":"1@c.us","sender":{"formattedName":"+55 11 9876"}}""", "+55 11 9876")]
    [InlineData("""{"from":"1@c.us","notifyName":"Caio"}""", "Caio")]
    [InlineData("""{"from":"1@c.us"}""", null)]
    public void ProfileName_ShouldFallBack(string json, string? expected)
    {
        Assert.Equal(expected, WhatsappSender.FromPayload(Parse(json)).ProfileName);
    }

    [Fact]
    public void ProfileName_ShouldBeCutAt120()
    {
        var longName = new string('a', 200);
        var json = "{\"from\":\"1@c.us\",\"sender\":{\"pushname\":\"" + longName + "\"}}";
        var sender = WhatsappSender.FromPayload(Parse(json));

        Assert.Equal(120, sender.ProfileName!.Length);
    }
}
