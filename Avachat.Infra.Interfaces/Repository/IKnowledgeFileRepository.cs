namespace Avachat.Infra.Interfaces.Repository;

public interface IKnowledgeFileRepository<T> where T : class
{
    Task<List<T>> GetByAgentIdAsync(long agentId);
    Task<T?> GetByIdAsync(long id);
    Task<T> CreateAsync(T file);
    Task<T> UpdateAsync(T file);
    Task DeleteAsync(long id);
}
