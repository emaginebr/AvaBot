using Microsoft.EntityFrameworkCore;
using Avachat.Domain.Models;
using Avachat.Infra.Context;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Infra.Repository;

public class ChatMessageRepository : IChatMessageRepository<ChatMessage>
{
    private readonly AvachatContext _context;

    public ChatMessageRepository(AvachatContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessage>> GetBySessionIdAsync(long sessionId, int page, int pageSize)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountBySessionIdAsync(long sessionId)
    {
        return await _context.ChatMessages.CountAsync(m => m.ChatSessionId == sessionId);
    }

    public async Task<List<ChatMessage>> GetRecentBySessionIdAsync(long sessionId, int count)
    {
        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        message.CreatedAt = DateTime.UtcNow;
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }
}
