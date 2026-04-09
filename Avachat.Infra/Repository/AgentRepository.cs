using Microsoft.EntityFrameworkCore;
using Avachat.Domain.Models;
using Avachat.Infra.Context;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Infra.Repository;

public class AgentRepository : IAgentRepository<Agent>
{
    private readonly AvachatContext _context;

    public AgentRepository(AvachatContext context)
    {
        _context = context;
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        return await _context.Agents.OrderByDescending(a => a.CreatedAt).ToListAsync();
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        return await _context.Agents.FirstOrDefaultAsync(a => a.AgentId == id);
    }

    public async Task<Agent?> GetBySlugAsync(string slug)
    {
        return await _context.Agents.FirstOrDefaultAsync(a => a.Slug == slug);
    }

    public async Task<Agent> CreateAsync(Agent agent)
    {
        agent.CreatedAt = DateTime.UtcNow;
        agent.UpdatedAt = DateTime.UtcNow;
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task<Agent> UpdateAsync(Agent agent)
    {
        agent.UpdatedAt = DateTime.UtcNow;
        _context.Agents.Update(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task DeleteAsync(long id)
    {
        var agent = await GetByIdAsync(id);
        if (agent != null)
        {
            // Delete children first to avoid FK violations
            var sessions = await _context.ChatSessions
                .Where(s => s.AgentId == id)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.ChatSessionId).ToList();
            if (sessionIds.Count > 0)
            {
                var messages = await _context.ChatMessages
                    .Where(m => sessionIds.Contains(m.ChatSessionId!.Value))
                    .ToListAsync();
                _context.ChatMessages.RemoveRange(messages);
            }

            _context.ChatSessions.RemoveRange(sessions);

            var files = await _context.KnowledgeFiles
                .Where(f => f.AgentId == id)
                .ToListAsync();
            _context.KnowledgeFiles.RemoveRange(files);

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, long? excludeId = null)
    {
        return await _context.Agents.AnyAsync(a => a.Slug == slug && (excludeId == null || a.AgentId != excludeId));
    }
}
