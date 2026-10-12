using System.Text;
using Microsoft.Extensions.DependencyInjection;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

/// <summary>Arquivo inexistente ou de outro agente; o painel responde "Arquivo nao encontrado", o MCP "Documento nao encontrado".</summary>
public class KnowledgeFileNotFoundException : KeyNotFoundException
{
    public KnowledgeFileNotFoundException() : base(KnowledgeFileService.FileNotFoundMessage) { }
}

/// <summary>
/// Base de conhecimento do painel e do MCP (feature 017, research R7): validacao de dono, limites e
/// ingestao em segundo plano no mesmo lugar, para que upload e adicionar_documento facam a mesma coisa.
/// </summary>
public class KnowledgeFileService
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const string AgentNotFoundMessage = "Agente nao encontrado";
    public const string FileNotFoundMessage = "Arquivo nao encontrado";
    public const string FileNameRequiredMessage = "Nome do arquivo e obrigatorio";
    public const string ContentRequiredMessage = "Conteudo e obrigatorio";
    public const string TooLargeMessage = "Documento excede o limite de 10MB";

    private readonly IAgentRepository<Agent> _agentRepository;
    private readonly IKnowledgeFileRepository<KnowledgeFile> _fileRepository;
    private readonly IElasticsearchService _esService;
    private readonly IServiceScopeFactory _scopeFactory;

    public KnowledgeFileService(
        IAgentRepository<Agent> agentRepository,
        IKnowledgeFileRepository<KnowledgeFile> fileRepository,
        IElasticsearchService esService,
        IServiceScopeFactory scopeFactory)
    {
        _agentRepository = agentRepository;
        _fileRepository = fileRepository;
        _esService = esService;
        _scopeFactory = scopeFactory;
    }

    private async Task EnsureOwnedAsync(long agentId, long ownerUserId)
    {
        if (await _agentRepository.GetByIdAsync(agentId, ownerUserId) == null)
            throw new KeyNotFoundException(AgentNotFoundMessage);
    }

    private async Task<KnowledgeFile> GetOwnedFileAsync(long agentId, long ownerUserId, long fileId)
    {
        await EnsureOwnedAsync(agentId, ownerUserId);

        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || file.AgentId != agentId)
            throw new KnowledgeFileNotFoundException();

        return file;
    }

    public async Task<List<KnowledgeFile>> ListAsync(long agentId, long ownerUserId)
    {
        await EnsureOwnedAsync(agentId, ownerUserId);
        return await _fileRepository.GetByAgentIdAsync(agentId);
    }

    /// <summary>Cria o documento a partir de texto (.md acrescentado se faltar) e dispara a ingestao em segundo plano.</summary>
    public async Task<KnowledgeFile> AddTextAsync(long agentId, long ownerUserId, string fileName, string content)
    {
        await EnsureOwnedAsync(agentId, ownerUserId);

        var name = (fileName ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException(FileNameRequiredMessage);
        if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            name += ".md";

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(ContentRequiredMessage);

        var size = Encoding.UTF8.GetByteCount(content);
        if (size > MaxFileSizeBytes)
            throw new InvalidOperationException(TooLargeMessage);

        var knowledgeFile = new KnowledgeFile
        {
            AgentId = agentId,
            FileName = name,
            FileContent = content,
            FileSize = size,
            ProcessingStatus = ProcessingStatus.Processing
        };

        knowledgeFile = await _fileRepository.CreateAsync(knowledgeFile);
        StartIngestion(knowledgeFile.KnowledgeFileId);
        return knowledgeFile;
    }

    public async Task DeleteAsync(long agentId, long ownerUserId, long fileId)
    {
        var file = await GetOwnedFileAsync(agentId, ownerUserId, fileId);

        await _esService.DeleteChunksByFileIdAsync(file.KnowledgeFileId);
        await _fileRepository.DeleteAsync(file.KnowledgeFileId);
    }

    public async Task<KnowledgeFile> ReprocessAsync(long agentId, long ownerUserId, long fileId)
    {
        var file = await GetOwnedFileAsync(agentId, ownerUserId, fileId);

        file.ProcessingStatus = ProcessingStatus.Processing;
        file.ErrorMessage = null;
        await _fileRepository.UpdateAsync(file);

        StartIngestion(file.KnowledgeFileId);
        return file;
    }

    // Processa em segundo plano com escopo proprio (o DbContext da requisicao nao sobrevive a ela).
    private void StartIngestion(long fileId)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
            await ingestionService.ProcessFileAsync(fileId);
        });
    }
}
