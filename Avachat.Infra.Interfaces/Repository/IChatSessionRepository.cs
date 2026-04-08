using Avachat.Domain.Models;

namespace Avachat.Infra.Interfaces.Repository;

public interface IChatSessionRepository
{
    Task<List<ChatSession>> GetByAgentIdAsync(long agentId, int page, int pageSize);
    Task<int> CountByAgentIdAsync(long agentId);
    Task<ChatSession?> GetByIdAsync(long id);
    Task<ChatSession> CreateAsync(ChatSession session);
    Task<ChatSession> UpdateAsync(ChatSession session);
}
