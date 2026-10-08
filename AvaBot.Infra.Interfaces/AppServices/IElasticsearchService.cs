namespace AvaBot.Infra.Interfaces.AppServices;

public interface IElasticsearchService
{
    Task CreateIndexAsync();
    Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks);
    Task DeleteChunksByFileIdAsync(long knowledgeFileId);
    Task DeleteChunksByAgentIdAsync(long agentId);
    Task<List<string>> TextSearchAsync(long agentId, string queryText, int topK = 5);
}

public class ChunkData
{
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public int ChunkIndex { get; set; }
}
