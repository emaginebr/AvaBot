using AvaBot.Infra.Interfaces.Repository;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Application.Services;

public class SearchService
{
    private readonly IKnowledgeChunkRepository _chunkRepository;

    public SearchService(IKnowledgeChunkRepository chunkRepository)
    {
        _chunkRepository = chunkRepository;
    }

    public async Task<List<string>> SearchAsync(long agentId, string query, int topK = 5)
    {
        return await _chunkRepository.TextSearchAsync(agentId, query, topK);
    }
}
