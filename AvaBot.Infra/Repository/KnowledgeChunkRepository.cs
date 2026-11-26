using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AvaBot.Infra.Repository;

/// <summary>
/// Trechos dos arquivos de conhecimento no PostgreSQL, com busca textual nativa (tsvector + GIN,
/// configuracao portuguese). Substitui o indice knowledge_chunks do Elasticsearch (feature 021).
/// </summary>
public class KnowledgeChunkRepository : IKnowledgeChunkRepository
{
    public const string SearchFailedMessage = "Não foi possível consultar a base de conhecimento.";

    private readonly AvaBotContext _context;
    private readonly ILogger<KnowledgeChunkRepository> _logger;

    public KnowledgeChunkRepository(AvaBotContext context, ILogger<KnowledgeChunkRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks)
    {
        if (chunks.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var chunk in chunks)
        {
            var entity = new KnowledgeChunk
            {
                AgentId = agentId,
                KnowledgeFileId = knowledgeFileId,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                Embedding = chunk.Embedding,
                CreatedAt = now
            };
            _context.KnowledgeChunks.Add(entity);
            _context.Entry(entity).Property(AvaBotContext.SearchContentProperty).CurrentValue =
                KnowledgeSearchText.Normalize(chunk.Content);
        }

        await _context.SaveChangesAsync();

        // Os trechos nao sao lidos de volta pelo EF; solta o rastreamento (embeddings sao grandes).
        foreach (var entry in _context.ChangeTracker.Entries<KnowledgeChunk>().ToList())
            entry.State = EntityState.Detached;

        _logger.LogInformation(
            "[KnowledgeChunks] {Count} trecho(s) gravado(s) - AgentId={AgentId}, FileId={FileId}",
            chunks.Count, agentId, knowledgeFileId);
    }

    public async Task DeleteChunksByFileIdAsync(long knowledgeFileId)
    {
        var deleted = await _context.KnowledgeChunks
            .Where(c => c.KnowledgeFileId == knowledgeFileId)
            .ExecuteDeleteAsync();

        _logger.LogInformation(
            "[KnowledgeChunks] Trechos removidos do FileId={FileId} - Deletados={Deleted}", knowledgeFileId, deleted);
    }

    public async Task DeleteChunksByAgentIdAsync(long agentId)
    {
        var deleted = await _context.KnowledgeChunks
            .Where(c => c.AgentId == agentId)
            .ExecuteDeleteAsync();

        _logger.LogInformation(
            "[KnowledgeChunks] Trechos removidos do AgentId={AgentId} - Deletados={Deleted}", agentId, deleted);
    }

    public async Task<List<string>> TextSearchAsync(long agentId, string queryText, int topK = 5)
    {
        var tsQuery = KnowledgeSearchText.BuildOrQuery(queryText);

        _logger.LogInformation(
            "[KnowledgeChunks] Busca textual - AgentId={AgentId}, Query=\"{Query}\", TopK={TopK}",
            agentId, queryText.Length > 100 ? queryText[..100] + "..." : queryText, topK);

        // Sem termo util nao ha o que casar (FR-003): nao devolve "todos os trechos do agente".
        if (tsQuery == null || topK <= 0) return new List<string>();

        List<string> results;
        try
        {
            // OR entre os termos (como o match do Elasticsearch); stopwords somem no to_tsquery.
            results = await _context.Database.SqlQuery<string>($"""
                SELECT c.content AS "Value"
                FROM avabot_knowledge_chunks c
                CROSS JOIN to_tsquery('portuguese'::regconfig, {tsQuery}) q
                WHERE c.agent_id = {agentId} AND c.search_vector @@ q
                ORDER BY ts_rank(c.search_vector, q) DESC, c.knowledge_file_id, c.chunk_index
                LIMIT {topK}
                """).ToListAsync();
        }
        catch (Exception ex)
        {
            // Falha nao pode virar lista vazia: seria indistinguivel de "o agente nao tem esse conteudo".
            _logger.LogWarning(ex, "[KnowledgeChunks] Busca textual falhou - AgentId={AgentId}", agentId);
            throw new InvalidOperationException(SearchFailedMessage, ex);
        }

        _logger.LogInformation(
            "[KnowledgeChunks] Busca concluida - AgentId={AgentId}, Resultados={Count}", agentId, results.Count);

        return results;
    }

    public async Task<List<long>> GetFileIdsMissingChunksAsync()
    {
        return await _context.KnowledgeFiles
            .Where(f => f.ProcessingStatus == ProcessingStatus.Ready
                && !_context.KnowledgeChunks.Any(c => c.KnowledgeFileId == f.KnowledgeFileId))
            .OrderBy(f => f.KnowledgeFileId)
            .Select(f => f.KnowledgeFileId)
            .ToListAsync();
    }
}
