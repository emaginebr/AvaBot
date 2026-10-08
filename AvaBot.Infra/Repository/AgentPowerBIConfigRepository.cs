using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class AgentPowerBIConfigRepository : IAgentPowerBIConfigRepository<AgentPowerBIConfig>
{
    private readonly AvaBotContext _context;

    public AgentPowerBIConfigRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<AgentPowerBIConfig?> GetByAgentIdAsync(long agentId)
    {
        return await _context.AgentPowerBIConfigs.FirstOrDefaultAsync(c => c.AgentId == agentId);
    }

    public async Task<AgentPowerBIConfig> UpsertAsync(AgentPowerBIConfig config)
    {
        var existing = await _context.AgentPowerBIConfigs
            .FirstOrDefaultAsync(c => c.AgentId == config.AgentId);

        if (existing == null)
        {
            config.CreatedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;
            _context.AgentPowerBIConfigs.Add(config);
            await _context.SaveChangesAsync();
            return config;
        }

        existing.TenantId = config.TenantId;
        existing.ClientId = config.ClientId;
        existing.ClientSecretEncrypted = config.ClientSecretEncrypted;
        existing.ClientSecretHint = config.ClientSecretHint;
        existing.LastTestAt = config.LastTestAt;
        existing.LastTestSuccess = config.LastTestSuccess;
        existing.LastTestMessage = config.LastTestMessage;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }
}
