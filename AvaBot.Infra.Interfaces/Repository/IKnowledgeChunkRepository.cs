namespace AvaBot.Infra.Interfaces.Repository;

/// <summary>
/// Trechos dos arquivos de conhecimento e a busca textual sobre eles (feature 021: substitui o Elasticsearch).
/// </summary>
public interface IKnowledgeChunkRepository
{
    /// <summary>Insere os trechos do arquivo de uma vez. Nao apaga os anteriores: quem chama apaga antes.</summary>
    Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks);
    Task DeleteChunksByFileIdAsync(long knowledgeFileId);
    Task DeleteChunksByAgentIdAsync(long agentId);
    /// <summary>
    /// Trechos do agente mais relevantes para a pergunta (no maximo <paramref name="topK"/>). Pergunta sem
    /// termos uteis devolve lista vazia; falha do banco lanca, nunca vira lista vazia.
    /// </summary>
    Task<List<string>> TextSearchAsync(long agentId, string queryText, int topK = 5);
    /// <summary>Arquivos prontos (Ready) que ainda nao tem nenhum trecho, em ordem crescente de id.</summary>
    Task<List<long>> GetFileIdsMissingChunksAsync();
}

public class ChunkData
{
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public int ChunkIndex { get; set; }
}
