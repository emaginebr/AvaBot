using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Application.Services;

public class SearchService
{
    private readonly IElasticsearchService _esService;

    public SearchService(IElasticsearchService esService)
    {
        _esService = esService;
    }

    public async Task<List<string>> SearchAsync(long agentId, string query, int topK = 5)
    {
        return await _esService.TextSearchAsync(agentId, query, topK);
    }
}
