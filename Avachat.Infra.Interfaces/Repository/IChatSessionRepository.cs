namespace Avachat.Infra.Interfaces.Repository;

public interface IChatSessionRepository<T> where T : class
{
    Task<List<T>> GetByAgentIdAsync(long agentId, int page, int pageSize);
    Task<int> CountByAgentIdAsync(long agentId);
    Task<T?> GetByIdAsync(long id);
    Task<T> CreateAsync(T session);
    Task<T> UpdateAsync(T session);
    Task<T?> GetByResumeTokenAsync(string resumeToken);
}
