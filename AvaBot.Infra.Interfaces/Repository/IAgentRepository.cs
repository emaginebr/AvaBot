namespace AvaBot.Infra.Interfaces.Repository;

public interface IAgentRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(long id);
    Task<T?> GetBySlugAsync(string slug);
    Task<T> CreateAsync(T agent);
    Task<T> UpdateAsync(T agent);
    Task DeleteAsync(long id);
    Task<bool> SlugExistsAsync(string slug, long? excludeId = null);
    Task<T?> GetByTelegramBotTokenAsync(string token, long? excludeId = null);
}
