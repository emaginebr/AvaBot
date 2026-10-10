using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using AvaBot.API.Controllers;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Controllers;

public class FileControllerTest
{
    private readonly Mock<IKnowledgeFileRepository<KnowledgeFile>> _fileRepoMock;
    private readonly Mock<IElasticsearchService> _esServiceMock;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock;
    private const long OwnerId = 7;
    private readonly IMapper _mapper;
    private readonly FileController _sut;

    public FileControllerTest()
    {
        _fileRepoMock = new Mock<IKnowledgeFileRepository<KnowledgeFile>>();
        _esServiceMock = new Mock<IElasticsearchService>();
        _agentRepoMock = new Mock<IAgentRepository<Agent>>();
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<KnowledgeFileProfile>();
        expr.AddProfile<AgentProfile>();
        _mapper = new MapperConfiguration(expr, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var agentService = new AgentService(_agentRepoMock.Object, _esServiceMock.Object, new Mock<ISecretProtector>().Object, _mapper);

        // Por padrao os agentes 1 e 10 sao do usuario do token; o 99 nao e.
        _agentRepoMock.Setup(r => r.GetByIdAsync(1, OwnerId)).ReturnsAsync(new Agent { AgentId = 1, OwnerUserId = OwnerId });
        _agentRepoMock.Setup(r => r.GetByIdAsync(10, OwnerId)).ReturnsAsync(new Agent { AgentId = 10, OwnerUserId = OwnerId });
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);

        _sut = new FileController(
            _fileRepoMock.Object,
            _esServiceMock.Object,
            scopeFactoryMock.Object,
            agentService,
            _mapper).WithUser(OwnerId);
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
    [Fact]
    public async Task GetByAgent_ShouldReturnNotFound_WhenAgentBelongsToAnotherOwner()
    {
        // FR-011: mesma resposta de agente inexistente; o repositorio de arquivos nem e consultado
        var result = await _sut.GetByAgent(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Agente nao encontrado", Assert.IsType<Result<object>>(notFound.Value).Mensagem);
        _fileRepoMock.Verify(r => r.GetByAgentIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenAgentBelongsToAnotherOwner()
    {
        _fileRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new KnowledgeFile { KnowledgeFileId = 1, AgentId = 99 });

        var result = await _sut.Delete(99, 1);

        Assert.IsType<NotFoundObjectResult>(result);
        _fileRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
    }
}