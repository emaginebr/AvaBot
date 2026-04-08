using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.Application.Services;

public class SearchService
{
    private readonly IElasticsearchService _esService;
    private readonly IOpenAIService _openAIService;

    public SearchService(IElasticsearchService esService, IOpenAIService openAIService)
    {
        _esService = esService;
        _openAIService = openAIService;
    }

    public async Task<List<string>> SearchAsync(long agentId, string query, int topK = 5)
    {
        var queryVector = await _openAIService.GenerateEmbeddingAsync(query);
        return await _esService.HybridSearchAsync(agentId, queryVector, query, topK);
    }
}
