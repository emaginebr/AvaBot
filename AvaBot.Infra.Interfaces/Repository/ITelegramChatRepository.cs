namespace AvaBot.Infra.Interfaces.Repository;

public interface ITelegramChatRepository<T> where T : class
{
    Task<T?> GetByChatIdAsync(long telegramChatId);
    Task<T> CreateAsync(T chat);
    Task<T> UpdateAsync(T chat);
    /// <summary>Entre as sessoes informadas, quais vieram do Telegram (campo channel do MCP, feature 017).</summary>
    Task<HashSet<long>> GetSessionIdsAsync(IEnumerable<long> chatSessionIds);
}
