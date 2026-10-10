using AvaBot.Calibration;
using AvaBot.Calibration.Local;

namespace AvaBot.Tests.Calibration;

public class LocalKnowledgeBaseTest : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "avabot-kb-" + Guid.NewGuid().ToString("N"));

    public LocalKnowledgeBaseTest()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "sub"));
        File.WriteAllText(Path.Combine(_folder, "tilapia.md"), "A tilápia é o peixe mais cultivado no Brasil.\n\nExportação de tilápia cresceu.");
        File.WriteAllText(Path.Combine(_folder, "sub", "camarao.txt"), "O camarão é exportado congelado.");
        File.WriteAllText(Path.Combine(_folder, "ignorado.pdf"), "tilápia tilápia tilápia");
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task TextSearchAsync_ShouldRankByMatchAndIgnoreChunksWithoutTerms()
    {
        var kb = LocalKnowledgeBase.Load(_folder, new[] { ".md", ".txt" }, 2000, 200);

        var results = await kb.TextSearchAsync(1, "Tilápia exportação");

        Assert.Equal(2, kb.FileCount);
        Assert.Single(results);
        Assert.Contains("tilápia", results[0]);
    }

    [Fact]
    public async Task TextSearchAsync_ShouldReturnEmpty_WhenFolderDoesNotExist()
    {
        var kb = LocalKnowledgeBase.Load(Path.Combine(_folder, "nao-existe"), new[] { ".md" }, 2000, 200);

        Assert.Empty(await kb.TextSearchAsync(1, "tilápia"));
        Assert.Equal(0, kb.ChunkCount);
    }

    [Fact]
    public void ConversationFile_ShouldNameConversationsAndRejectEmptyOnes()
    {
        var conversations = ConversationFile.Parse("""
            { "conversations": [ { "messages": ["a", " b "] }, { "name": "x", "messages": ["c"] } ] }
            """);

        Assert.Equal("conversa-1", conversations[0].Name);
        Assert.Equal(new[] { "a", "b" }, conversations[0].Messages);
        Assert.Equal("x", conversations[1].Name);
        Assert.Throws<InvalidOperationException>(() => ConversationFile.Parse("""{ "conversations": [ { "messages": [] } ] }"""));
    }

    [Fact]
    public void CommandLine_ShouldParseOptionsAndRejectQuestionWithFile()
    {
        var command = CommandLine.Parse(new[] { "-q", "pergunta", "-o", "saida.md", "--refresh-schema" });

        Assert.Equal("pergunta", command.Question);
        Assert.Equal("saida.md", command.OutputPath);
        Assert.True(command.RefreshSchema);
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(new[] { "-q", "a", "-f", "b.json" }));
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(new[] { "--xyz" }));
    }
}
