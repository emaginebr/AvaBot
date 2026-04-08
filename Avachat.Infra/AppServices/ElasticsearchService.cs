using System.Runtime.CompilerServices;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Configuration;
using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.Infra.AppServices;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;

    public ElasticsearchService(IConfiguration configuration)
    {
        var url = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        _indexName = configuration["Elasticsearch:IndexName"] ?? "knowledge_chunks";
        var settings = new ElasticsearchClientSettings(new Uri(url));
        _client = new ElasticsearchClient(settings);
    }

    public async Task CreateIndexAsync()
    {
        var exists = await _client.Indices.ExistsAsync(_indexName);
        if (exists.Exists) return;

        await _client.Indices.CreateAsync(_indexName, c => c
            .Mappings(m => m
                .Properties(p => p
                    .Keyword("chunk_id")
                    .Keyword("agent_id")
                    .Keyword("knowledge_file_id")
                    .Text("content", t => t.Analyzer("standard"))
                    .DenseVector("embedding", dv => dv
                        .Dims(1536)
                        .Index(true)
                        .Similarity(DenseVectorSimilarity.Cosine))
                    .IntegerNumber("chunk_index")
                )
            )
        );
    }

    public async Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var doc = new Dictionary<string, object>
            {
                ["chunk_id"] = $"{knowledgeFileId}_{chunk.ChunkIndex}",
                ["agent_id"] = agentId.ToString(),
                ["knowledge_file_id"] = knowledgeFileId.ToString(),
                ["content"] = chunk.Content,
                ["embedding"] = chunk.Embedding,
                ["chunk_index"] = chunk.ChunkIndex
            };

            await _client.IndexAsync(doc, idx => idx
                .Index(_indexName)
                .Id($"{knowledgeFileId}_{chunk.ChunkIndex}")
            );
        }

        await _client.Indices.RefreshAsync(_indexName);
    }

    public async Task DeleteChunksByFileIdAsync(long knowledgeFileId)
    {
        await _client.DeleteByQueryAsync(_indexName, d => d
            .Query(q => q
                .Term(t => t
                    .Field("knowledge_file_id")
                    .Value(knowledgeFileId.ToString())
                )
            )
        );
    }

    public async Task DeleteChunksByAgentIdAsync(long agentId)
    {
        await _client.DeleteByQueryAsync(_indexName, d => d
            .Query(q => q
                .Term(t => t
                    .Field("agent_id")
                    .Value(agentId.ToString())
                )
            )
        );
    }

    public async Task<List<string>> HybridSearchAsync(long agentId, float[] queryVector, string queryText, int topK = 5)
    {
        var response = await _client.SearchAsync<Dictionary<string, object>>(s => s
            .Indices(_indexName)
            .Size(topK)
            .Query(q => q
                .Bool(b => b
                    .Filter(f => f
                        .Term(t => t
                            .Field("agent_id")
                            .Value(agentId.ToString())
                        )
                    )
                    .Should(
                        sh => sh.Match(m => m
                            .Field("content")
                            .Query(queryText)
                        )
                    )
                )
            )
            .Knn(k => k
                .Field("embedding")
                .QueryVector(queryVector)
                .K(topK)
                .NumCandidates(topK * 10)
                .Filter(f => f
                    .Term(t => t
                        .Field("agent_id")
                        .Value(agentId.ToString())
                    )
                )
            )
        );

        var results = new List<string>();
        if (response.IsValidResponse && response.Documents != null)
        {
            foreach (var doc in response.Documents)
            {
                if (doc.TryGetValue("content", out var content))
                {
                    results.Add(content?.ToString() ?? string.Empty);
                }
            }
        }

        return results;
    }
}
