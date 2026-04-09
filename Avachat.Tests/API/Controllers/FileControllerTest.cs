using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Avachat.API.Controllers;
using Avachat.Application.Profiles;
using Avachat.Application.Services;
using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.DTO;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Tests.API.Controllers;

public class FileControllerTest
{
    private readonly Mock<IKnowledgeFileRepository<KnowledgeFile>> _fileRepoMock;
    private readonly Mock<IElasticsearchService> _esServiceMock;
    private readonly IMapper _mapper;
    private readonly FileController _sut;

    public FileControllerTest()
    {
        _fileRepoMock = new Mock<IKnowledgeFileRepository<KnowledgeFile>>();
        _esServiceMock = new Mock<IElasticsearchService>();
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<KnowledgeFileProfile>();
        _mapper = new MapperConfiguration(expr, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        _sut = new FileController(
            _fileRepoMock.Object,
            _esServiceMock.Object,
            scopeFactoryMock.Object,
            _mapper);
    }

    [Fact]
    public async Task GetByAgent_ShouldReturnOk_WithFileList()
    {
        // Arrange
        var files = new List<KnowledgeFile>
        {
            new() { KnowledgeFileId = 1, AgentId = 10, FileName = "test.md", FileSize = 100 }
        };
        _fileRepoMock.Setup(r => r.GetByAgentIdAsync(10)).ReturnsAsync(files);

        // Act
        var result = await _sut.GetByAgent(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<List<KnowledgeFileInfo>>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.Single(response.Dados!);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsNull()
    {
        // Act
        var result = await _sut.Upload(1, null!);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<Result<object>>(badResult.Value);
        Assert.False(response.Sucesso);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenNotMdFile()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.Length).Returns(100);

        // Act
        var result = await _sut.Upload(1, fileMock.Object);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<Result<object>>(badResult.Value);
        Assert.Contains(".md", response.Mensagem);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileTooLarge()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.md");
        fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024); // 11MB

        // Act
        var result = await _sut.Upload(1, fileMock.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenFileNotExists()
    {
        // Arrange
        _fileRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((KnowledgeFile?)null);

        // Act
        var result = await _sut.Delete(1, 999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenFileAgentMismatch()
    {
        // Arrange
        var file = new KnowledgeFile { KnowledgeFileId = 1, AgentId = 99 };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);

        // Act
        var result = await _sut.Delete(10, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenFileExistsAndMatches()
    {
        // Arrange
        var file = new KnowledgeFile { KnowledgeFileId = 1, AgentId = 10 };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);

        // Act
        var result = await _sut.Delete(10, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _esServiceMock.Verify(s => s.DeleteChunksByFileIdAsync(1), Times.Once);
        _fileRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Reprocess_ShouldReturnNotFound_WhenFileNotExists()
    {
        // Arrange
        _fileRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((KnowledgeFile?)null);

        // Act
        var result = await _sut.Reprocess(1, 999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Reprocess_ShouldReturnOk_WhenFileExistsAndMatches()
    {
        // Arrange
        var file = new KnowledgeFile { KnowledgeFileId = 1, AgentId = 10 };
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(file);

        // Act
        var result = await _sut.Reprocess(10, 1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
