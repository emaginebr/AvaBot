using Xunit;
using ModelContextProtocol.Protocol;
using AvaBot.API.Mcp;

namespace AvaBot.Tests.API.Mcp;

public class McpSecretGuardFilterTest
{
    public static IEnumerable<object[]> ForbiddenNamesInEveryCase()
    {
        foreach (var name in McpSecretGuardFilter.ForbiddenKeys)
        {
            yield return [name];
            yield return [name.ToLowerInvariant()];
            yield return [name.ToUpperInvariant()];
        }
    }

    [Theory]
    [MemberData(nameof(ForbiddenNamesInEveryCase))]
    public async Task ShouldRefuseTheWholeCall_WhenAnyForbiddenFieldIsPresent(string fieldName)
    {
        var nextCalled = false;
        var handler = McpSecretGuardFilter.Create()((_, _) =>
        {
            nextCalled = true;
            return new ValueTask<CallToolResult>(McpToolResults.Ok(new { ok = true }));
        });
        var context = McpTestContext.Context("atualizar_agente",
            McpTestContext.Arguments(("agentId", 12), ("systemPrompt", "novo"), (fieldName, "sk-123")));

        var result = await handler(context, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.False(nextCalled);
        var text = McpTestContext.Text(result);
        Assert.Contains($"'{fieldName}'", text);
        Assert.Contains("painel", text);
        Assert.Contains("Nenhum campo desta chamada foi gravado", text);
    }

    [Fact]
    public async Task ShouldInvokeNext_WhenOnlyAllowedFieldsArePresent()
    {
        var handler = McpSecretGuardFilter.Create()(McpTestContext.Next(() => McpToolResults.Ok(new { ok = true })));
        var context = McpTestContext.Context("criar_agente",
            McpTestContext.Arguments(("agentId", 12), ("name", "Suporte"), ("systemPrompt", "p"), ("telegramBotName", "bot")));

        var result = await handler(context, CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
    }

    [Fact]
    public async Task ShouldInvokeNext_WhenThereAreNoArguments()
    {
        var handler = McpSecretGuardFilter.Create()(McpTestContext.Next(() => McpToolResults.Ok(new { ok = true })));
        var context = McpTestContext.Context("quem_sou_eu");

        var result = await handler(context, CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
    }
}
