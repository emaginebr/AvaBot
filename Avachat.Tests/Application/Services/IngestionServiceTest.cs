using Xunit;
using Moq;
using Avachat.Application.Services;
using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Tests.Application.Services;

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
        _sut = new IngestionService(_fileRepoMock.Object, _esServiceMock.Object, _openAIServiceMock.Object);
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
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });

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
        var text = string.Join("\n\n", Enumerable.Range(1, 100).Select(i => $"Paragraph {i} with content."));

        var result = IngestionService.ChunkText(text, 100, 20);

        foreach (var chunk in result)
        {
            Assert.True(chunk.Length <= 200, $"Chunk too large: {chunk.Length} chars");
        }
    }
}
