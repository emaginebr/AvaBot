using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

public class KnowledgeFileServiceTest
{
    private const long OwnerId = 7;
    private const long AgentId = 10;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IKnowledgeFileRepository<KnowledgeFile>> _fileRepoMock = new();
    private readonly Mock<IElasticsearchService> _esMock = new();
    private readonly KnowledgeFileService _sut;

    public KnowledgeFileServiceTest()
    {
        // O escopo de ingestao e um mock: o Task.Run em segundo plano falha em silencio no teste.
        var scopeFactory = new Mock<IServiceScopeFactory>();
        _sut = new KnowledgeFileService(_agentRepoMock.Object, _fileRepoMock.Object, _esMock.Object, scopeFactory.Object);

        _agentRepoMock.Setup(r => r.GetByIdAsync(AgentId, OwnerId)).ReturnsAsync(new Agent { AgentId = AgentId, OwnerUserId = OwnerId });
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);
        _fileRepoMock.Setup(r => r.CreateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => { f.KnowledgeFileId = 1; return f; });
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>())).ReturnsAsync((KnowledgeFile f) => f);
    }

    [Fact]
    public async Task AddTextAsync_ShouldAppendMdExtension_AndStoreSizeInBytes()
    {
        var file = await _sut.AddTextAsync(AgentId, OwnerId, "politica", "Trocas em ate 30 dias.");

        Assert.Equal("politica.md", file.FileName);
        Assert.Equal(AgentId, file.AgentId);
        Assert.Equal(ProcessingStatus.Processing, file.ProcessingStatus);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount("Trocas em ate 30 dias."), file.FileSize);
        _fileRepoMock.Verify(r => r.CreateAsync(It.IsAny<KnowledgeFile>()), Times.Once);
    }

    [Fact]
    public async Task AddTextAsync_ShouldKeepTheMdExtension_IgnoringCase()
    {
        var file = await _sut.AddTextAsync(AgentId, OwnerId, "Politica.MD", "x");

        Assert.Equal("Politica.MD", file.FileName);
    }

    [Fact]
    public async Task AddTextAsync_ShouldRejectDocumentsOverTenMegabytes_WithoutSaving()
    {
        var content = new string('a', 10 * 1024 * 1024 + 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddTextAsync(AgentId, OwnerId, "grande.md", content));

        Assert.Equal("Documento excede o limite de 10MB", ex.Message);
        _fileRepoMock.Verify(r => r.CreateAsync(It.IsAny<KnowledgeFile>()), Times.Never);
    }

    [Fact]
    public async Task AddTextAsync_ShouldRejectAnEmptyNameOrContent()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddTextAsync(AgentId, OwnerId, "  ", "x"));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddTextAsync(AgentId, OwnerId, "a.md", " "));
    }

    [Fact]
    public async Task AddTextAsync_ShouldThrowNotFound_ForAForeignAgent()
    {
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.AddTextAsync(99, OwnerId, "a.md", "x"));

        Assert.Equal("Agente nao encontrado", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowFileNotFound_WhenTheFileBelongsToAnotherAgent()
    {
        _fileRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new KnowledgeFile { KnowledgeFileId = 5, AgentId = 99 });

        var ex = await Assert.ThrowsAsync<KnowledgeFileNotFoundException>(() => _sut.DeleteAsync(AgentId, OwnerId, 5));

        Assert.Equal("Arquivo nao encontrado", ex.Message);
        _fileRepoMock.Verify(r => r.DeleteAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveChunksAndTheRecord()
    {
        _fileRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new KnowledgeFile { KnowledgeFileId = 5, AgentId = AgentId });

        await _sut.DeleteAsync(AgentId, OwnerId, 5);

        _esMock.Verify(s => s.DeleteChunksByFileIdAsync(5), Times.Once);
        _fileRepoMock.Verify(r => r.DeleteAsync(5), Times.Once);
    }

    [Fact]
    public async Task ReprocessAsync_ShouldResetTheStatusToProcessing()
    {
        var file = new KnowledgeFile { KnowledgeFileId = 5, AgentId = AgentId, ProcessingStatus = ProcessingStatus.Error, ErrorMessage = "falhou" };
        _fileRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(file);

        var result = await _sut.ReprocessAsync(AgentId, OwnerId, 5);

        Assert.Equal(ProcessingStatus.Processing, result.ProcessingStatus);
        Assert.Null(result.ErrorMessage);
        _fileRepoMock.Verify(r => r.UpdateAsync(file), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ShouldThrowNotFound_ForAForeignAgent_WithoutQueryingFiles()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ListAsync(99, OwnerId));

        _fileRepoMock.Verify(r => r.GetByAgentIdAsync(It.IsAny<long>()), Times.Never);
    }
}
