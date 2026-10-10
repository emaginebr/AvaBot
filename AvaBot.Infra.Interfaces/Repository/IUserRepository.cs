namespace AvaBot.Infra.Interfaces.Repository;

public interface IUserRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id);
    /// <summary>Busca pelo e-mail ja normalizado (User.NormalizeEmail).</summary>
    Task<T?> GetByEmailAsync(string normalizedEmail);
    Task<bool> EmailExistsAsync(string normalizedEmail);
    Task<int> CountAsync();
    Task<T> CreateAsync(T user);
    Task<T> UpdateAsync(T user);
}
