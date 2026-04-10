namespace Avachat.Infra.Interfaces.Repository;

public interface IChatMessageRepository<T> where T : class
{
    Task<List<T>> GetBySessionIdAsync(long sessionId, int page, int pageSize);
    Task<int> CountBySessionIdAsync(long sessionId);
    Task<List<T>> GetRecentBySessionIdAsync(long sessionId, int count);
    Task<T> CreateAsync(T message);
    Task<List<T>> GetLastBySessionIdAsync(long sessionId, int count = 10);
}
