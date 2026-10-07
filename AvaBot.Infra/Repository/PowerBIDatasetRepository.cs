using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class PowerBIDatasetRepository : IPowerBIDatasetRepository<PowerBIDataset>
{
    private readonly AvaBotContext _context;

    public PowerBIDatasetRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<List<PowerBIDataset>> GetByAgentIdAsync(long agentId)
    {
        return await _context.PowerBIDatasets
            .Where(d => d.AgentId == agentId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<PowerBIDataset?> GetByIdAsync(long agentId, long id)
    {
        return await _context.PowerBIDatasets
            .FirstOrDefaultAsync(d => d.AgentId == agentId && d.PowerBIDatasetId == id);
    }

    public async Task<bool> ExistsAsync(long agentId, string datasetId, long? excludeId = null)
    {
        return await _context.PowerBIDatasets
            .AnyAsync(d => d.AgentId == agentId
                && d.DatasetId == datasetId
                && (excludeId == null || d.PowerBIDatasetId != excludeId));
    }

    public async Task<bool> ToolKeyExistsAsync(long agentId, string toolKey, long? excludeId = null)
    {
        return await _context.PowerBIDatasets
            .AnyAsync(d => d.AgentId == agentId
                && d.ToolKey == toolKey
                && (excludeId == null || d.PowerBIDatasetId != excludeId));
    }

    public async Task<PowerBIDataset> CreateAsync(PowerBIDataset dataset)
    {
        dataset.CreatedAt = DateTime.UtcNow;
        dataset.UpdatedAt = DateTime.UtcNow;
        _context.PowerBIDatasets.Add(dataset);
        await _context.SaveChangesAsync();
        return dataset;
    }

    public async Task<PowerBIDataset> UpdateAsync(PowerBIDataset dataset)
    {
        dataset.UpdatedAt = DateTime.UtcNow;
        _context.PowerBIDatasets.Update(dataset);
        await _context.SaveChangesAsync();
        return dataset;
    }

    public async Task DeleteAsync(long id)
    {
        var dataset = await _context.PowerBIDatasets.FirstOrDefaultAsync(d => d.PowerBIDatasetId == id);
        if (dataset != null)
        {
            _context.PowerBIDatasets.Remove(dataset);
            await _context.SaveChangesAsync();
        }
    }
}
