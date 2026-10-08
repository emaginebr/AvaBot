using Xunit;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

public class IngestionServiceTest
{
    private readonly Mock<IKnowledgeFileRepository<KnowledgeFile>> _fileRepoMock;
    private readonly Mock<IElasticsearchService> _esServiceMock;
    private readonly Mock<IOpenAIService> _openAIServiceMock;
    private readonly IngestionService _sut;

    public IngestionServiceTest()
    {
        _fileRepoMock = new Mock<IKnowledgeFileRepository<KnowledgeFile>>();
        _esServiceMock = new Mock<IElasticsearchService>();
        _openAIServiceMock = new Mock<IOpenAIService>();
        _sut = new IngestionService(_fileRepoMock.Object, _esServiceMock.Object, _openAIServiceMock.Object, NullLogger<IngestionService>.Instance);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldDoNothing_WhenFileNotFound()
    {
        // Arrange
        _fileRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((KnowledgeFile?)null);

        // Act
        await _sut.ProcessFileAsync(999);

        // Assert
        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSetStatusReady_WhenSuccessful()
    {
        // Arrange
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1,
            AgentId = 10,
            FileContent = "Simple content",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        // Act
        await _sut.ProcessFileAsync(1);

        // Assert
        Assert.Equal(ProcessingStatus.Ready, file.ProcessingStatus);
        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(1), Times.Once);
        _esServiceMock.Verify(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSetStatusError_WhenExceptionOccurs()
    {
        // Arrange
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1,
            AgentId = 10,
            FileContent = "Content",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _esServiceMock.Setup(s => s.DeleteChunksByFileIdAsync(1)).ThrowsAsync(new Exception("ES error"));

        // Act
        await _sut.ProcessFileAsync(1);

        // Assert
        Assert.Equal(ProcessingStatus.Error, file.ProcessingStatus);
        Assert.Equal("ES error", file.ErrorMessage);
    }

    // --- Upload: chunks are sent to Elasticsearch ---

    [Fact]
    public async Task ProcessFileAsync_ShouldSendCorrectNumberOfChunks_ToElasticsearch()
    {
        var content = "Paragrafo 1 com conteudo.\n\nParagrafo 2 com conteudo.\n\nParagrafo 3 com conteudo.";
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 5, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f, 0.2f });

        await _sut.ProcessFileAsync(5);

        var expectedChunks = IngestionService.ChunkText(content, 2000, 200);

        _esServiceMock.Verify(s => s.IndexChunksAsync(
            10, 5,
            It.Is<List<ChunkData>>(list => list.Count == expectedChunks.Count)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendChunksWithCorrectContent()
    {
        var content = "Primeiro paragrafo do documento.\n\nSegundo paragrafo do documento.";
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.5f });

        List<ChunkData>? capturedChunks = null;
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback<long, long, List<ChunkData>>((_, _, chunks) => capturedChunks = chunks);

        await _sut.ProcessFileAsync(1);

        Assert.NotNull(capturedChunks);
        Assert.Contains(capturedChunks, c => c.Content.Contains("Primeiro paragrafo"));
        Assert.Contains(capturedChunks, c => c.Content.Contains("Segundo paragrafo"));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendChunksWithEmbeddings()
    {
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo simples",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(embedding);

        List<ChunkData>? capturedChunks = null;
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback<long, long, List<ChunkData>>((_, _, chunks) => capturedChunks = chunks);

        await _sut.ProcessFileAsync(1);

        Assert.NotNull(capturedChunks);
        Assert.All(capturedChunks, c => Assert.Equal(embedding, c.Embedding));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendChunksWithCorrectIndexes()
    {
        var content = string.Join("\n\n", Enumerable.Range(1, 5).Select(i => $"Paragrafo {i} com conteudo suficiente."));
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        List<ChunkData>? capturedChunks = null;
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback<long, long, List<ChunkData>>((_, _, chunks) => capturedChunks = chunks);

        await _sut.ProcessFileAsync(1);

        Assert.NotNull(capturedChunks);
        for (int i = 0; i < capturedChunks.Count; i++)
        {
            Assert.Equal(i, capturedChunks[i].ChunkIndex);
        }
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldGenerateEmbeddingForEachChunk()
    {
        var content = "Paragrafo A.\n\nParagrafo B.\n\nParagrafo C.";
        var expectedChunks = IngestionService.ChunkText(content, 2000, 200);
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(1);

        _openAIServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Exactly(expectedChunks.Count));
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldDeleteOldChunks_BeforeIndexingNew()
    {
        var callOrder = new List<string>();
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        _esServiceMock.Setup(s => s.DeleteChunksByFileIdAsync(1))
            .Callback(() => callOrder.Add("delete"));
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .Callback(() => callOrder.Add("index"));

        await _sut.ProcessFileAsync(1);

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("delete", callOrder[0]);
        Assert.Equal("index", callOrder[1]);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSendCorrectAgentIdAndFileId()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 42, AgentId = 99, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(42);

        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(42), Times.Once);
        _esServiceMock.Verify(s => s.IndexChunksAsync(99, 42, It.IsAny<List<ChunkData>>()), Times.Once);
    }

    // --- Delete: chunks are removed from Elasticsearch ---

    [Fact]
    public async Task ProcessFileAsync_ShouldDeleteChunksFromElasticsearch_WhenReprocessing()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 7, AgentId = 10, FileContent = "Reprocessar",
            ProcessingStatus = ProcessingStatus.Ready
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(7);

        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(7), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldNotIndexChunks_WhenDeleteFails()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _esServiceMock.Setup(s => s.DeleteChunksByFileIdAsync(1)).ThrowsAsync(new Exception("ES delete error"));

        await _sut.ProcessFileAsync(1);

        _esServiceMock.Verify(s => s.IndexChunksAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<List<ChunkData>>()), Times.Never);
        Assert.Equal(ProcessingStatus.Error, file.ProcessingStatus);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldSetError_WhenIndexingFails()
    {
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = "Conteudo",
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });
        _esServiceMock.Setup(s => s.IndexChunksAsync(10, 1, It.IsAny<List<ChunkData>>()))
            .ThrowsAsync(new Exception("Index failed"));

        await _sut.ProcessFileAsync(1);

        Assert.Equal(ProcessingStatus.Error, file.ProcessingStatus);
        Assert.Equal("Index failed", file.ErrorMessage);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldHandleLargeDocument_WithMultipleChunks()
    {
        var content = string.Join("\n\n", Enumerable.Range(1, 50).Select(i =>
            $"Paragrafo {i}: " + new string('x', 100)));
        var expectedChunks = IngestionService.ChunkText(content, 2000, 200);
        var file = new KnowledgeFile
        {
            KnowledgeFileId = 1, AgentId = 10, FileContent = content,
            ProcessingStatus = ProcessingStatus.Processing
        };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

        await _sut.ProcessFileAsync(1);

        _esServiceMock.Verify(s => s.IndexChunksAsync(10, 1,
            It.Is<List<ChunkData>>(list => list.Count == expectedChunks.Count)), Times.Once);
        _openAIServiceMock.Verify(s => s.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Exactly(expectedChunks.Count));
        Assert.Equal(ProcessingStatus.Ready, file.ProcessingStatus);
    }

    // --- ChunkText static method tests ---

    [Fact]
    public void ChunkText_ShouldReturnEmpty_WhenTextIsEmpty()
    {
        var result = IngestionService.ChunkText("");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShouldReturnEmpty_WhenTextIsWhitespace()
    {
        var result = IngestionService.ChunkText("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void ChunkText_ShouldReturnSingleChunk_WhenTextIsSmall()
    {
        var result = IngestionService.ChunkText("Hello world");
        Assert.Single(result);
        Assert.Equal("Hello world", result[0]);
    }

    [Fact]
    public void ChunkText_ShouldSplitOnDoubleNewlines()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 50).Select(i => $"Paragraph {i} with some content to fill space."));

        var result = IngestionService.ChunkText(text, 200, 50);

        Assert.True(result.Count > 1);
    }

    [Fact]
    public void ChunkText_ShouldRespectChunkSize()
    {
        var text = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Paragraph {i} with content."));

        var result = IngestionService.ChunkText(text, 100, 20);

        Assert.True(result.Count > 1);
    }

    [Fact]
    public void ChunkText_ShouldNotBreakLines_InTheMiddle()
    {
        var text = "Linha 1 completa\nLinha 2 completa\nLinha 3 completa\nLinha 4 completa";

        var result = IngestionService.ChunkText(text, 40, 0);

        foreach (var chunk in result)
        {
            Assert.DoesNotContain("complet\n", chunk);
            var lines = chunk.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Assert.True(
                        line.StartsWith("Linha") || line.EndsWith("completa"),
                        $"Linha quebrada no meio: \"{line}\"");
                }
            }
        }
    }

    [Fact]
    public void ChunkText_ShouldSplitOnSingleNewlines()
    {
        var text = "Linha 1\nLinha 2\nLinha 3\nLinha 4\nLinha 5\nLinha 6\nLinha 7\nLinha 8\nLinha 9\nLinha 10";

        var result = IngestionService.ChunkText(text, 30, 0);

        Assert.True(result.Count > 1);
        foreach (var chunk in result)
        {
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                Assert.StartsWith("Linha", line);
            }
        }
    }

    [Fact]
    public void ChunkText_ShouldPreserveAllContent()
    {
        var lines = Enumerable.Range(1, 20).Select(i => $"Linha {i} do documento").ToList();
        var text = string.Join("\n", lines);

        var result = IngestionService.ChunkText(text, 80, 0);
        var allContent = string.Join("\n", result);

        foreach (var line in lines)
        {
            Assert.Contains(line, allContent);
        }
    }

    [Fact]
    public void ChunkText_ShouldHandleOverlap_WithCompleteLines()
    {
        var text = "Linha 1\nLinha 2\nLinha 3\nLinha 4\nLinha 5\nLinha 6";

        var result = IngestionService.ChunkText(text, 30, 15);

        Assert.True(result.Count > 1);
        foreach (var chunk in result)
        {
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                Assert.StartsWith("Linha", line);
            }
        }
    }

    [Fact]
    public void ChunkText_ShouldHandleMarkdownDocument()
    {
        var text = "# Titulo\n\nParagrafo de introducao.\n\n## Secao 1\n\nConteudo da secao 1.\nContinuacao da secao 1.\n\n## Secao 2\n\nConteudo da secao 2.";

        var result = IngestionService.ChunkText(text, 60, 0);

        Assert.True(result.Count > 1);
        foreach (var chunk in result)
        {
            Assert.DoesNotContain("Titul\n", chunk);
            Assert.DoesNotContain("seca\n", chunk);
        }
    }
}
