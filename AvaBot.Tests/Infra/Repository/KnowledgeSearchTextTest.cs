using AvaBot.Infra.Repository;
using Xunit;

namespace AvaBot.Tests.Infra.Repository;

public class KnowledgeSearchTextTest
{
    [Theory]
    [InlineData("Ação", "acao")]
    [InlineData("AÇÃO", "acao")]
    [InlineData("Horário de Atendimento", "horario de atendimento")]
    [InlineData("pão, maçã e café", "pao, maca e cafe")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_ShouldLowercaseAndRemoveAccents(string? input, string expected)
    {
        Assert.Equal(expected, KnowledgeSearchText.Normalize(input));
    }

    [Fact]
    public void Normalize_ShouldGiveSameResult_ForAccentedAndUnaccentedText()
    {
        Assert.Equal(KnowledgeSearchText.Normalize("Informações"), KnowledgeSearchText.Normalize("informacoes"));
    }

    [Fact]
    public void BuildOrQuery_ShouldJoinNormalizedTermsWithOr()
    {
        Assert.Equal("qual | o | horario | da | loja", KnowledgeSearchText.BuildOrQuery("Qual o horário da loja?"));
    }

    [Fact]
    public void BuildOrQuery_ShouldRemoveDuplicateTerms()
    {
        Assert.Equal("loja | abre", KnowledgeSearchText.BuildOrQuery("loja LOJA abre loja"));
    }

    [Theory]
    [InlineData("a & b", "a | b")]
    [InlineData("!segredo:*", "segredo")]
    [InlineData("(x | y) <-> z", "x | y | z")]
    [InlineData("'; DROP TABLE avabot_agents; --", "drop | table | avabot | agents")]
    public void BuildOrQuery_ShouldStripTsQueryOperators(string input, string expected)
    {
        Assert.Equal(expected, KnowledgeSearchText.BuildOrQuery(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?!...")]
    [InlineData(null)]
    public void BuildOrQuery_ShouldReturnNull_WhenThereAreNoTerms(string? input)
    {
        Assert.Null(KnowledgeSearchText.BuildOrQuery(input));
    }

    [Fact]
    public void BuildOrQuery_ShouldLimitNumberOfTerms()
    {
        var input = string.Join(' ', Enumerable.Range(1, 200).Select(i => $"termo{i}"));

        var query = KnowledgeSearchText.BuildOrQuery(input)!;

        Assert.Equal(KnowledgeSearchText.MaxTerms, query.Split(" | ").Length);
    }

    [Fact]
    public void BuildOrQuery_ShouldKeepNumbers()
    {
        Assert.Equal("cnpj | 12 | 345", KnowledgeSearchText.BuildOrQuery("CNPJ 12.345"));
    }
}
