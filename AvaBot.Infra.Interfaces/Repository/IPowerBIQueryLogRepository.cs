namespace AvaBot.Infra.Interfaces.Repository;

public interface IPowerBIQueryLogRepository<T> where T : class
{
    Task<T> CreateAsync(T log);
    Task<(List<T> Items, int Total)> GetPagedByAgentAsync(long agentId, int page, int pageSize);
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
}
