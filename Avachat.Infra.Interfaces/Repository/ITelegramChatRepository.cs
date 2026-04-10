namespace Avachat.Infra.Interfaces.Repository;

public interface ITelegramChatRepository<T> where T : class
{
    Task<T?> GetByChatIdAsync(long telegramChatId);
    Task<T> CreateAsync(T chat);
    Task<T> UpdateAsync(T chat);
}
