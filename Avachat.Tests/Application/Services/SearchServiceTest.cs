using Xunit;
using Moq;
using Avachat.Application.Services;
using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.Tests.Application.Services;

public class SearchServiceTest
{
    private readonly Mock<IElasticsearchService> _esServiceMock;
    private readonly Mock<IOpenAIService> _openAIServiceMock;
    private readonly SearchService _sut;

    public SearchServiceTest()
    {
        _esServiceMock = new Mock<IElasticsearchService>();
        _openAIServiceMock = new Mock<IOpenAIService>();
        _sut = new SearchService(_esServiceMock.Object, _openAIServiceMock.Object);
    }

    [Fact]
    public async Task SearchAsync_ShouldGenerateEmbeddingAndSearch()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var chunks = new List<string> { "chunk 1", "chunk 2" };

        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync("test query")).ReturnsAsync(embedding);
        _esServiceMock.Setup(s => s.HybridSearchAsync(1, embedding, "test query", 5)).ReturnsAsync(chunks);

        // Act
        var result = await _sut.SearchAsync(1, "test query");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("chunk 1", result[0]);
        _openAIServiceMock.Verify(s => s.GenerateEmbeddingAsync("test query"), Times.Once);
        _esServiceMock.Verify(s => s.HybridSearchAsync(1, embedding, "test query", 5), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldPassCustomTopK()
    {
        // Arrange
        var embedding = new float[] { 0.1f };
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(embedding);
        _esServiceMock.Setup(s => s.HybridSearchAsync(1, embedding, "q", 10)).ReturnsAsync(new List<string>());

        // Act
        await _sut.SearchAsync(1, "q", 10);

        // Assert
        _esServiceMock.Verify(s => s.HybridSearchAsync(1, embedding, "q", 10), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoResults()
    {
        // Arrange
        _openAIServiceMock.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>())).ReturnsAsync(new float[] { 0.1f });
        _esServiceMock.Setup(s => s.HybridSearchAsync(It.IsAny<long>(), It.IsAny<float[]>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.SearchAsync(1, "no results");

        // Assert
        Assert.Empty(result);
    }
}
