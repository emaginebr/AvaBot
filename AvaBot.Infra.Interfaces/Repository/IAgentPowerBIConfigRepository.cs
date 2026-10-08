namespace AvaBot.Infra.Interfaces.Repository;

public interface IAgentPowerBIConfigRepository<T> where T : class
{
    Task<T?> GetByAgentIdAsync(long agentId);
    Task<T> UpsertAsync(T config);
}
