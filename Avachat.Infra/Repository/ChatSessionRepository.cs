using Microsoft.EntityFrameworkCore;
using Avachat.Domain.Models;
using Avachat.Infra.Context;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Infra.Repository;

public class ChatSessionRepository : IChatSessionRepository<ChatSession>
{
    private readonly AvachatContext _context;

    public ChatSessionRepository(AvachatContext context)
    {
        _context = context;
    }

    public async Task<List<ChatSession>> GetByAgentIdAsync(long agentId, int page, int pageSize)
    {
        return await _context.ChatSessions
            .Where(s => s.AgentId == agentId)
            .OrderByDescending(s => s.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByAgentIdAsync(long agentId)
    {
        return await _context.ChatSessions.CountAsync(s => s.AgentId == agentId);
    }

    public async Task<ChatSession?> GetByIdAsync(long id)
    {
        return await _context.ChatSessions.FirstOrDefaultAsync(s => s.ChatSessionId == id);
    }

    public async Task<ChatSession> CreateAsync(ChatSession session)
    {
        session.StartedAt = DateTime.UtcNow;
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<ChatSession> UpdateAsync(ChatSession session)
    {
        _context.ChatSessions.Update(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<ChatSession?> GetByResumeTokenAsync(string resumeToken)
    {
        return await _context.ChatSessions
            .Include(s => s.Agent)
            .FirstOrDefaultAsync(s => s.ResumeToken == resumeToken);
    }
}
