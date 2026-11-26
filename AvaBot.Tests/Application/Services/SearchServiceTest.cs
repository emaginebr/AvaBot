using AvaBot.Infra.Interfaces.Repository;
using Xunit;
using Moq;
using AvaBot.Application.Services;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Tests.Application.Services;

public class SearchServiceTest
{
    private readonly Mock<IKnowledgeChunkRepository> _chunkRepositoryMock;
    private readonly SearchService _sut;

    public SearchServiceTest()
    {
        _chunkRepositoryMock = new Mock<IKnowledgeChunkRepository>();
        _sut = new SearchService(_chunkRepositoryMock.Object);
    }

    [Fact]
    public async Task SearchAsync_ShouldSearchByTextWithoutEmbedding()
    {
        // Arrange
        var chunks = new List<string> { "chunk 1", "chunk 2" };
        _chunkRepositoryMock.Setup(s => s.TextSearchAsync(1, "test query", 5)).ReturnsAsync(chunks);

        // Act
        var result = await _sut.SearchAsync(1, "test query");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("chunk 1", result[0]);
        _chunkRepositoryMock.Verify(s => s.TextSearchAsync(1, "test query", 5), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldPassCustomTopK()
    {
        // Arrange
        _chunkRepositoryMock.Setup(s => s.TextSearchAsync(1, "q", 10)).ReturnsAsync(new List<string>());

        // Act
        await _sut.SearchAsync(1, "q", 10);

        // Assert
        _chunkRepositoryMock.Verify(s => s.TextSearchAsync(1, "q", 10), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoResults()
    {
        // Arrange
        _chunkRepositoryMock.Setup(s => s.TextSearchAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.SearchAsync(1, "no results");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_ShouldPropagateFailure_WhenChunkSearchFails()
    {
        // Arrange: falha do servidor nao pode virar lista vazia (contrato search-api.md)
        _chunkRepositoryMock.Setup(s => s.TextSearchAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Não foi possível consultar a base de conhecimento."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SearchAsync(1, "q"));
    }
}
