using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Infra.AppServices;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;
    private readonly ILogger<ElasticsearchService> _logger;
    private bool _indexCreated;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        var url = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        _indexName = configuration["Elasticsearch:IndexName"] ?? "knowledge_chunks";
        _logger = logger;
        var settings = new ElasticsearchClientSettings(new Uri(url));
        _client = new ElasticsearchClient(settings);
    }

    public async Task CreateIndexAsync()
    {
        if (_indexCreated) return;

        for (int attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var exists = await _client.Indices.ExistsAsync(_indexName);
                if (exists.Exists)
                {
                    _indexCreated = true;
                    return;
                }

                var properties = new Properties
                {
                    ["chunk_id"] = new KeywordProperty(),
                    ["agent_id"] = new KeywordProperty(),
                    ["knowledge_file_id"] = new KeywordProperty(),
                    ["content"] = new TextProperty { Analyzer = "standard" },
                    ["embedding"] = new DenseVectorProperty
                    {
                        Dims = 1536,
                        Index = true,
                        Similarity = DenseVectorSimilarity.Cosine
                    },
                    ["chunk_index"] = new IntegerNumberProperty()
                };

                var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                    .Mappings(m => m.Properties(properties))
                );

                if (createResponse.IsValidResponse)
                {
                    _indexCreated = true;
                    Console.WriteLine($"[Elasticsearch] Index '{_indexName}' created successfully.");
                    return;
                }

                Console.WriteLine($"[Elasticsearch] Attempt {attempt}/10 - Failed to create index: {createResponse.DebugInformation}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Elasticsearch] Attempt {attempt}/10 - Connection error: {ex.Message}");
            }

            if (attempt < 10)
                await Task.Delay(3000);
        }

        Console.WriteLine($"[Elasticsearch] WARNING: Could not create index '{_indexName}' after 10 attempts.");
    }

    private async Task EnsureIndexAsync()
    {
        if (!_indexCreated)
            await CreateIndexAsync();
    }

    public async Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks)
    {
        await EnsureIndexAsync();

        _logger.LogInformation(
            "[Elasticsearch] Indexando {Count} chunk(s) - AgentId={AgentId}, FileId={FileId}, Index={Index}",
            chunks.Count, agentId, knowledgeFileId, _indexName);

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var docId = $"{knowledgeFileId}_{chunk.ChunkIndex}";
            var doc = new Dictionary<string, object>
            {
                ["chunk_id"] = docId,
                ["agent_id"] = agentId.ToString(),
                ["knowledge_file_id"] = knowledgeFileId.ToString(),
                ["content"] = chunk.Content,
                ["embedding"] = chunk.Embedding,
                ["chunk_index"] = chunk.ChunkIndex
            };

            var response = await _client.IndexAsync(doc, idx => idx
                .Index(_indexName)
                .Id(docId)
            );

            _logger.LogInformation(
                "[Elasticsearch] Chunk [{Index}/{Total}] indexado - DocId={DocId}, Sucesso={Success}",
                i + 1, chunks.Count, docId, response.IsValidResponse);
        }

        await _client.Indices.RefreshAsync(_indexName);
        _logger.LogInformation("[Elasticsearch] Index refreshed apos indexacao de {Count} chunk(s)", chunks.Count);
    }

    public async Task DeleteChunksByFileIdAsync(long knowledgeFileId)
    {
        await EnsureIndexAsync();

        _logger.LogInformation("[Elasticsearch] Removendo chunks do FileId={FileId}", knowledgeFileId);

        var response = await _client.DeleteByQueryAsync<Dictionary<string, object>>(_indexName, d => d
            .Query(q => q
                .Term(new TermQuery("knowledge_file_id")
                {
                    Value = knowledgeFileId.ToString()
                })
            )
        );

        _logger.LogInformation(
            "[Elasticsearch] Chunks removidos do FileId={FileId} - Deletados={Deleted}",
            knowledgeFileId, response.Deleted);
    }

    public async Task DeleteChunksByAgentIdAsync(long agentId)
    {
        await EnsureIndexAsync();

        _logger.LogInformation("[Elasticsearch] Removendo todos os chunks do AgentId={AgentId}", agentId);

        var response = await _client.DeleteByQueryAsync<Dictionary<string, object>>(_indexName, d => d
            .Query(q => q
                .Term(new TermQuery("agent_id")
                {
                    Value = agentId.ToString()
                })
            )
        );

        _logger.LogInformation(
            "[Elasticsearch] Chunks removidos do AgentId={AgentId} - Deletados={Deleted}",
            agentId, response.Deleted);
    }

    public async Task<List<string>> TextSearchAsync(long agentId, string queryText, int topK = 5)
    {
        await EnsureIndexAsync();

        _logger.LogInformation(
            "[Elasticsearch] Busca textual - AgentId={AgentId}, Query=\"{Query}\", TopK={TopK}",
            agentId, queryText.Length > 100 ? queryText[..100] + "..." : queryText, topK);

        // D2: o match vai em Must, nao em Should. Um bool com filter + should tem
        // minimum_should_match zero, entao sem kNN ele devolveria todos os chunks
        // do agente mesmo sem nenhuma correspondencia no conteudo.
        var response = await _client.SearchAsync<Dictionary<string, object>>(s => s
            .Index(_indexName)
            .Size(topK)
            .Query(q => q
                .Bool(b => b
                    .Filter(new Query[]
                    {
                        new TermQuery("agent_id") { Value = agentId.ToString() }
                    })
                    .Must(new Query[]
                    {
                        new MatchQuery("content") { Query = queryText }
                    })
                )
            )
        );

        // Falha do Elasticsearch nao pode virar lista vazia: seria indistinguivel de
        // "o agente nao tem esse conteudo" na tela.
        if (!response.IsValidResponse)
        {
            _logger.LogWarning("[Elasticsearch] Busca textual falhou - AgentId={AgentId}", agentId);
            throw new InvalidOperationException(
                "Não foi possível consultar a base de conhecimento no Elasticsearch.");
        }

        var results = new List<string>();
        if (response.Documents != null)
        {
            foreach (var doc in response.Documents)
            {
                if (doc.TryGetValue("content", out var content))
                {
                    results.Add(content?.ToString() ?? string.Empty);
                }
            }
        }

        _logger.LogInformation(
            "[Elasticsearch] Busca concluida - AgentId={AgentId}, Resultados={Count}",
            agentId, results.Count);

        for (int i = 0; i < results.Count; i++)
        {
            var preview = results[i].Length > 150 ? results[i][..150] + "..." : results[i];
            _logger.LogInformation("[Elasticsearch] Resultado [{Index}]: {Preview}", i + 1, preview);
        }

        return results;
    }
}
