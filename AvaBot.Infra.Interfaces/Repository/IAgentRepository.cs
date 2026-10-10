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
    Task<T?> GetByWhatsappTokenAsync(string token, long? excludeId = null);

    // Filtro por dono (feature 016). Os metodos sem dono acima continuam para rotas publicas.
    Task<List<T>> GetAllByOwnerAsync(long ownerUserId);
    Task<T?> GetByIdAsync(long id, long ownerUserId);
    Task<T?> GetBySlugAsync(string slug, long ownerUserId);
    Task<int> CountWithoutOwnerAsync();
    Task<int> AssignOwnerToOrphansAsync(long ownerUserId);
}
