using System.Text.RegularExpressions;
using Xunit;
using AvaBot.Domain.Models;

namespace AvaBot.Tests.Domain;

public class McpAccessKeyTest
{
    [Fact]
    public void GenerateKey_ShouldHaveThePrefix_TheExpectedLength_AndOnlyBase64UrlChars()
    {
        var key = McpAccessKey.GenerateKey();

        Assert.StartsWith("avabot_mcp_", key);
        Assert.Equal(54, key.Length);
        Assert.Matches(new Regex("^[A-Za-z0-9_-]{43}$"), key["avabot_mcp_".Length..]);
        Assert.True(McpAccessKey.HasExpectedFormat(key));
    }

    [Fact]
    public void GenerateKey_ShouldProduceDifferentValuesEachTime()
    {
        Assert.NotEqual(McpAccessKey.GenerateKey(), McpAccessKey.GenerateKey());
    }

    [Fact]
    public void ComputeHash_ShouldBeDeterministic_AndLowercaseHex()
    {
        var key = McpAccessKey.GenerateKey();

        var first = McpAccessKey.ComputeHash(key);
        var second = McpAccessKey.ComputeHash(key);

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Matches(new Regex("^[0-9a-f]{64}$"), first);
        Assert.NotEqual(first, McpAccessKey.ComputeHash(key + "x"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIn0.abc")]
    [InlineData("avabot_mcp_curta")]
    [InlineData("outro_prefixo_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void HasExpectedFormat_ShouldBeFalse_ForAnythingThatIsNotAKey(string? value)
    {
        Assert.False(McpAccessKey.HasExpectedFormat(value));
    }

    [Fact]
    public void ExtractPrefix_ShouldReturnTheFirstEightCharsAfterThePrefix()
    {
        var key = "avabot_mcp_Ab3kQ9xZrestodachave_______________________";

        Assert.Equal("Ab3kQ9xZ", McpAccessKey.ExtractPrefix(key));
    }
}
