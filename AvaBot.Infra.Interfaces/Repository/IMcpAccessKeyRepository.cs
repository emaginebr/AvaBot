namespace AvaBot.Infra.Interfaces.Repository;

public interface IMcpAccessKeyRepository<T> where T : class
{
    /// <summary>Chave pelo id, filtrada pelo dono: id de outra conta responde como inexistente.</summary>
    Task<T?> GetByIdAsync(long id, long userId);
    /// <summary>Chave pelo hash SHA-256 do valor em claro, com o usuario dono carregado.</summary>
    Task<T?> GetByHashAsync(string keyHash);
    /// <summary>Todas as chaves da conta, mais recentes primeiro, ativas e revogadas.</summary>
    Task<List<T>> GetAllByUserAsync(long userId);
    Task<int> CountActiveByUserAsync(long userId);
    Task<T> CreateAsync(T key);
    Task<T> UpdateAsync(T key);
}
