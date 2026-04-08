using Avachat.Domain.Models;

namespace Avachat.Infra.Interfaces.Repository;

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> GetBySessionIdAsync(long sessionId, int page, int pageSize);
    Task<int> CountBySessionIdAsync(long sessionId);
    Task<List<ChatMessage>> GetRecentBySessionIdAsync(long sessionId, int count);
    Task<ChatMessage> CreateAsync(ChatMessage message);
}
