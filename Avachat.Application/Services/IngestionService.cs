using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Application.Services;

public class IngestionService
{
    private readonly IKnowledgeFileRepository<KnowledgeFile> _fileRepository;
    private readonly IElasticsearchService _esService;
    private readonly IOpenAIService _openAIService;

    public IngestionService(
        IKnowledgeFileRepository<KnowledgeFile> fileRepository,
        IElasticsearchService esService,
        IOpenAIService openAIService)
    {
        _fileRepository = fileRepository;
        _esService = esService;
        _openAIService = openAIService;
    }

    public async Task ProcessFileAsync(long fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null) return;

        try
        {
            file.ProcessingStatus = ProcessingStatus.Processing;
            file.ErrorMessage = null;
            await _fileRepository.UpdateAsync(file);

            // Delete old chunks if reprocessing
            await _esService.DeleteChunksByFileIdAsync(fileId);

            // Chunk the content
            var chunks = ChunkText(file.FileContent, 2000, 200);

            // Generate embeddings and index
            var chunkDataList = new List<ChunkData>();
            for (int i = 0; i < chunks.Count; i++)
            {
                var embedding = await _openAIService.GenerateEmbeddingAsync(chunks[i]);
                chunkDataList.Add(new ChunkData
                {
                    Content = chunks[i],
                    Embedding = embedding,
                    ChunkIndex = i
                });
            }

            await _esService.IndexChunksAsync(file.AgentId!.Value, fileId, chunkDataList);

            file.ProcessingStatus = ProcessingStatus.Ready;
            await _fileRepository.UpdateAsync(file);
        }
        catch (Exception ex)
        {
            file.ProcessingStatus = ProcessingStatus.Error;
            file.ErrorMessage = ex.Message;
            await _fileRepository.UpdateAsync(file);
        }
    }

    public static List<string> ChunkText(string text, int chunkSize = 2000, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        // Split on double newlines first for natural boundaries
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = string.Empty;

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length + 2 > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.Trim());
                // Keep overlap from end of previous chunk
                var overlapStart = Math.Max(0, currentChunk.Length - overlap);
                currentChunk = currentChunk[overlapStart..] + "\n\n" + paragraph;
            }
            else
            {
                currentChunk += (currentChunk.Length > 0 ? "\n\n" : "") + paragraph;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }

        return chunks;
    }
}
