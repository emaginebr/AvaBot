using Avachat.Domain.Models;

namespace Avachat.Infra.Interfaces.Repository;

public interface IAgentRepository
{
    Task<List<Agent>> GetAllAsync();
    Task<Agent?> GetByIdAsync(long id);
    Task<Agent?> GetBySlugAsync(string slug);
    Task<Agent> CreateAsync(Agent agent);
    Task<Agent> UpdateAsync(Agent agent);
    Task DeleteAsync(long id);
    Task<bool> SlugExistsAsync(string slug, long? excludeId = null);
}
