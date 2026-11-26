using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AvaBot.Tests.Application.Services;

public class KnowledgeChunkBackfillServiceTest
{
    private readonly Mock<IKnowledgeChunkRepository> _chunkRepositoryMock = new();
    private readonly Mock<IKnowledgeFileRepository<KnowledgeFile>> _fileRepoMock = new();
    private readonly Mock<IOpenAIService> _openAIMock = new();
    private readonly Dictionary<long, KnowledgeFile> _files = new();

    public KnowledgeChunkBackfillServiceTest()
    {
        _fileRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => _files.GetValueOrDefault(id));
        _fileRepoMock.Setup(r => r.UpdateAsync(It.IsAny<KnowledgeFile>()))
            .ReturnsAsync((KnowledgeFile f) => f);
        _openAIMock.Setup(o => o.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });
    }

    private KnowledgeChunkBackfillService CreateSut(Dictionary<string, string?>? settings = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_chunkRepositoryMock.Object);
        services.AddSingleton(_fileRepoMock.Object);
        services.AddSingleton(_openAIMock.Object);
        services.AddScoped<IngestionService>();
        var provider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? new()).Build();
        return new KnowledgeChunkBackfillService(
            provider.GetRequiredService<IServiceScopeFactory>(), configuration,
            NullLogger<KnowledgeChunkBackfillService>.Instance);
    }

    private KnowledgeFile AddFile(long id, string content) =>
        _files[id] = new KnowledgeFile
        {
            KnowledgeFileId = id,
            AgentId = 7,
            FileName = $"arquivo{id}.md",
            FileContent = content,
            ProcessingStatus = ProcessingStatus.Ready
        };

    [Fact]
    public async Task RunAsync_ShouldDoNothing_WhenNoFileIsMissingChunks()
    {
        _chunkRepositoryMock.Setup(r => r.GetFileIdsMissingChunksAsync()).ReturnsAsync(new List<long>());

        var result = await CreateSut().RunAsync();

        Assert.Equal(0, result.Total);
        _chunkRepositoryMock.Verify(r => r.IndexChunksAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<List<ChunkData>>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldReprocessEachFileFromStoredContent()
    {
        AddFile(1, "Horário de atendimento: 8h às 18h.");
        AddFile(2, "A loja fica na Rua A.");
        _chunkRepositoryMock.Setup(r => r.GetFileIdsMissingChunksAsync()).ReturnsAsync(new List<long> { 1, 2 });

        var result = await CreateSut().RunAsync();

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Migrated);
        Assert.Equal(0, result.Failed);
        _chunkRepositoryMock.Verify(r => r.IndexChunksAsync(7, 1,
            It.Is<List<ChunkData>>(c => c.Count == 1 && c[0].Content.Contains("Horário"))), Times.Once);
        _chunkRepositoryMock.Verify(r => r.IndexChunksAsync(7, 2, It.IsAny<List<ChunkData>>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldCountFailure_AndContinueWithNextFiles()
    {
        AddFile(1, "conteudo que falha");
        AddFile(2, "conteudo ok");
        _chunkRepositoryMock.Setup(r => r.GetFileIdsMissingChunksAsync()).ReturnsAsync(new List<long> { 1, 2 });
        _openAIMock.Setup(o => o.GenerateEmbeddingAsync(It.IsAny<long>(), "conteudo que falha"))
            .ThrowsAsync(new InvalidOperationException("sem chave OpenAI"));

        var result = await CreateSut().RunAsync();

        Assert.Equal(1, result.Migrated);
        Assert.Equal(1, result.Failed);
        Assert.Equal(ProcessingStatus.Error, _files[1].ProcessingStatus);
        Assert.Equal(ProcessingStatus.Ready, _files[2].ProcessingStatus);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipFile_DeletedDuringBackfill()
    {
        _chunkRepositoryMock.Setup(r => r.GetFileIdsMissingChunksAsync()).ReturnsAsync(new List<long> { 99 });

        var result = await CreateSut().RunAsync();

        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Migrated);
    }

    [Fact]
    public async Task StartAsync_ShouldNotRun_WhenDisabled()
    {
        var sut = CreateSut(new() { [KnowledgeChunkBackfillService.EnabledConfigKey] = "false" });

        await sut.StartAsync(CancellationToken.None);
        await (sut.ExecuteTask ?? Task.CompletedTask);

        _chunkRepositoryMock.Verify(r => r.GetFileIdsMissingChunksAsync(), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldIndexWithoutEmbeddings_WhenAgentHasNoOpenAIKey()
    {
        AddFile(1, "Primeiro parágrafo.\n\nSegundo parágrafo.");
        _chunkRepositoryMock.Setup(r => r.GetFileIdsMissingChunksAsync()).ReturnsAsync(new List<long> { 1 });
        _openAIMock.Setup(o => o.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new MissingOpenAIKeyException());

        var result = await CreateSut().RunAsync();

        Assert.Equal(1, result.Migrated);
        Assert.Equal(ProcessingStatus.Ready, _files[1].ProcessingStatus);
        _chunkRepositoryMock.Verify(r => r.IndexChunksAsync(7, 1,
            It.Is<List<ChunkData>>(c => c.Count > 0 && c.All(x => x.Embedding.Length == 0 && x.Content.Length > 0))), Times.Once);
        // Sem chave, nao adianta chamar a OpenAI de novo a cada trecho.
        _openAIMock.Verify(o => o.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_ShouldStillFail_WhenAgentHasNoOpenAIKey_OutsideBackfill()
    {
        AddFile(1, "conteudo");
        _openAIMock.Setup(o => o.GenerateEmbeddingAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new MissingOpenAIKeyException());
        var ingestion = new IngestionService(_fileRepoMock.Object, _chunkRepositoryMock.Object, _openAIMock.Object,
            NullLogger<IngestionService>.Instance);

        await ingestion.ProcessFileAsync(1);

        Assert.Equal(ProcessingStatus.Error, _files[1].ProcessingStatus);
        _chunkRepositoryMock.Verify(r => r.IndexChunksAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<List<ChunkData>>()), Times.Never);
    }
}
