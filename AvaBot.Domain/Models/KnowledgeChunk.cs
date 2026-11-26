namespace AvaBot.Domain.Models;

/// <summary>
/// Trecho de um arquivo de conhecimento, pesquisavel pela busca textual do banco (feature 021).
/// O texto normalizado e o tsvector sao colunas do banco (shadow properties no AvaBotContext).
/// </summary>
public class KnowledgeChunk
{
    public long KnowledgeChunkId { get; set; }
    public long AgentId { get; set; }
    public long KnowledgeFileId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public DateTime CreatedAt { get; set; }
}
