using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class AgentRepository : IAgentRepository<Agent>
{
    private readonly AvaBotContext _context;

    public AgentRepository(AvaBotContext context)
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

            var powerBILogs = await _context.PowerBIQueryLogs
                .Where(l => l.AgentId == id)
                .ToListAsync();
            _context.PowerBIQueryLogs.RemoveRange(powerBILogs);

            var powerBIDatasets = await _context.PowerBIDatasets
                .Where(d => d.AgentId == id)
                .ToListAsync();
            _context.PowerBIDatasets.RemoveRange(powerBIDatasets);

            var powerBIConfig = await _context.AgentPowerBIConfigs
                .FirstOrDefaultAsync(c => c.AgentId == id);
            if (powerBIConfig != null)
                _context.AgentPowerBIConfigs.Remove(powerBIConfig);

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> SlugExistsAsync(string slug, long? excludeId = null)
    {
        return await _context.Agents.AnyAsync(a => a.Slug == slug && (excludeId == null || a.AgentId != excludeId));
    }

    public async Task<Agent?> GetByTelegramBotTokenAsync(string token, long? excludeId = null)
    {
        return await _context.Agents.FirstOrDefaultAsync(a =>
            a.TelegramBotToken == token && (excludeId == null || a.AgentId != excludeId));
    }

    public async Task<Agent?> GetByWhatsappTokenAsync(string token, long? excludeId = null)
    {
        return await _context.Agents.FirstOrDefaultAsync(a =>
            a.WhatsappToken == token && (excludeId == null || a.AgentId != excludeId));
    }

    public async Task<List<Agent>> GetAllByOwnerAsync(long ownerUserId)
    {
        return await _context.Agents
            .Where(a => a.OwnerUserId == ownerUserId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Agent?> GetByIdAsync(long id, long ownerUserId)
    {
        return await _context.Agents.FirstOrDefaultAsync(a => a.AgentId == id && a.OwnerUserId == ownerUserId);
    }

    public async Task<Agent?> GetBySlugAsync(string slug, long ownerUserId)
    {
        return await _context.Agents.FirstOrDefaultAsync(a => a.Slug == slug && a.OwnerUserId == ownerUserId);
    }

    public async Task<int> CountWithoutOwnerAsync()
    {
        return await _context.Agents.CountAsync(a => a.OwnerUserId == null);
    }

    public async Task<int> AssignOwnerToOrphansAsync(long ownerUserId)
    {
        return await _context.Agents
            .Where(a => a.OwnerUserId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.OwnerUserId, ownerUserId));
    }
}
