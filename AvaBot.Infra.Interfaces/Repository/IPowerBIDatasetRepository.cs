namespace AvaBot.Infra.Interfaces.Repository;

public interface IPowerBIDatasetRepository<T> where T : class
{
    Task<List<T>> GetByAgentIdAsync(long agentId);
    Task<T?> GetByIdAsync(long agentId, long id);
    Task<bool> ExistsAsync(long agentId, string datasetId, long? excludeId = null);
    Task<bool> ToolKeyExistsAsync(long agentId, string toolKey, long? excludeId = null);
    Task<T> CreateAsync(T dataset);
    Task<T> UpdateAsync(T dataset);
    Task DeleteAsync(long id);
}
