using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Logging;

namespace AvaBot.Application.Services;

public class IngestionService
{
    private readonly IKnowledgeFileRepository<KnowledgeFile> _fileRepository;
    private readonly IElasticsearchService _esService;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IKnowledgeFileRepository<KnowledgeFile> fileRepository,
        IElasticsearchService esService,
        IOpenAIService openAIService,
        ILogger<IngestionService> logger)
    {
        _fileRepository = fileRepository;
        _esService = esService;
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task ProcessFileAsync(long fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            _logger.LogWarning("[Ingestion] Arquivo ID={FileId} nao encontrado", fileId);
            return;
        }

        _logger.LogInformation(
            "[Ingestion] Iniciando processamento do arquivo ID={FileId}, Nome={FileName}, AgentId={AgentId}, Tamanho={FileSize} bytes",
            fileId, file.FileName, file.AgentId, file.FileSize);

        try
        {
            file.ProcessingStatus = ProcessingStatus.Processing;
            file.ErrorMessage = null;
            await _fileRepository.UpdateAsync(file);

            // Delete old chunks if reprocessing
            _logger.LogInformation("[Ingestion] Removendo chunks antigos do arquivo ID={FileId} no Elasticsearch", fileId);
            await _esService.DeleteChunksByFileIdAsync(fileId);

            // Chunk the content
            var chunks = ChunkText(file.FileContent, 2000, 200);
            _logger.LogInformation(
                "[Ingestion] Arquivo ID={FileId} dividido em {ChunkCount} chunk(s) (tamanho=2000, overlap=200)",
                fileId, chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                var preview = chunks[i].Length > 150 ? chunks[i][..150] + "..." : chunks[i];
                _logger.LogInformation(
                    "[Ingestion] Chunk [{Index}/{Total}] ({Length} chars): {Preview}",
                    i + 1, chunks.Count, chunks[i].Length, preview);
            }

            // Generate embeddings and index
            var chunkDataList = new List<ChunkData>();
            for (int i = 0; i < chunks.Count; i++)
            {
                _logger.LogInformation(
                    "[Ingestion] Gerando embedding para chunk [{Index}/{Total}]...",
                    i + 1, chunks.Count);

                var embedding = await _openAIService.GenerateEmbeddingAsync(file.AgentId!.Value, chunks[i]);

                _logger.LogInformation(
                    "[Ingestion] Embedding gerado para chunk [{Index}/{Total}] - {Dimensions} dimensoes",
                    i + 1, chunks.Count, embedding.Length);

                chunkDataList.Add(new ChunkData
                {
                    Content = chunks[i],
                    Embedding = embedding,
                    ChunkIndex = i
                });
            }

            _logger.LogInformation(
                "[Ingestion] Indexando {ChunkCount} chunk(s) no Elasticsearch para AgentId={AgentId}, FileId={FileId}",
                chunkDataList.Count, file.AgentId, fileId);

            await _esService.IndexChunksAsync(file.AgentId!.Value, fileId, chunkDataList);

            file.ProcessingStatus = ProcessingStatus.Ready;
            await _fileRepository.UpdateAsync(file);

            _logger.LogInformation(
                "[Ingestion] Processamento concluido com sucesso para arquivo ID={FileId}, Nome={FileName} - {ChunkCount} chunk(s) indexados",
                fileId, file.FileName, chunkDataList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Ingestion] Erro ao processar arquivo ID={FileId}, Nome={FileName}: {Error}",
                fileId, file.FileName, ex.Message);

            file.ProcessingStatus = ProcessingStatus.Error;
            file.ErrorMessage = ex.Message;
            await _fileRepository.UpdateAsync(file);
        }
    }

    public static List<string> ChunkText(string text, int chunkSize = 2000, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Priority 1: split by paragraphs (double newline)
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None);

        var currentChunk = string.Empty;

        foreach (var paragraph in paragraphs)
        {
            // If a single paragraph exceeds chunkSize, split it by lines
            if (paragraph.Length > chunkSize)
            {
                // Flush current chunk first
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.Trim());
                    currentChunk = BuildOverlap(currentChunk, overlap, "\n\n");
                }

                // Split large paragraph by individual lines
                SplitByLines(paragraph, chunkSize, overlap, chunks, ref currentChunk);
                continue;
            }

            var separator = currentChunk.Length > 0 ? "\n\n" : "";
            var candidate = currentChunk + separator + paragraph;

            if (candidate.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.Trim());
                currentChunk = BuildOverlap(currentChunk, overlap, "\n\n") + "\n\n" + paragraph;
            }
            else
            {
                currentChunk = candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }

        return chunks;
    }

    private static void SplitByLines(string text, int chunkSize, int overlap, List<string> chunks, ref string currentChunk)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var separator = currentChunk.Length > 0 ? "\n" : "";
            var candidate = currentChunk + separator + line;

            if (candidate.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.Trim());
                currentChunk = BuildOverlap(currentChunk, overlap, "\n") + "\n" + line;
            }
            else
            {
                currentChunk = candidate;
            }
        }
    }

    private static string BuildOverlap(string text, int maxOverlap, string separator)
    {
        if (maxOverlap <= 0 || string.IsNullOrEmpty(text)) return string.Empty;

        var parts = text.Split(new[] { separator }, StringSplitOptions.None);
        var result = string.Empty;

        for (int i = parts.Length - 1; i >= 0; i--)
        {
            var candidate = i == parts.Length - 1
                ? parts[i]
                : parts[i] + separator + result;

            if (candidate.Length > maxOverlap)
                break;

            result = candidate;
        }

        return result;
    }
}
