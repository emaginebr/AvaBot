using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class PowerBIQueryLogRepository : IPowerBIQueryLogRepository<PowerBIQueryLog>
{
    private readonly AvaBotContext _context;

    public PowerBIQueryLogRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<PowerBIQueryLog> CreateAsync(PowerBIQueryLog log)
    {
        log.CreatedAt = DateTime.UtcNow;
        _context.PowerBIQueryLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<(List<PowerBIQueryLog> Items, int Total)> GetPagedByAgentAsync(long agentId, int page, int pageSize)
    {
        var query = _context.PowerBIQueryLogs.Where(l => l.AgentId == agentId);
        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff)
    {
        var oldLogs = await _context.PowerBIQueryLogs
            .Where(l => l.CreatedAt < cutoff)
            .ToListAsync();

        if (oldLogs.Count == 0) return 0;

        _context.PowerBIQueryLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
        return oldLogs.Count;
    }
}
