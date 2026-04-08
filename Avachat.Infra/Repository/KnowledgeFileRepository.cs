using Microsoft.EntityFrameworkCore;
using Avachat.Domain.Models;
using Avachat.Infra.Context;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Infra.Repository;

public class KnowledgeFileRepository : IKnowledgeFileRepository<KnowledgeFile>
{
    private readonly AvachatContext _context;

    public KnowledgeFileRepository(AvachatContext context)
    {
        _context = context;
    }

    public async Task<List<KnowledgeFile>> GetByAgentIdAsync(long agentId)
    {
        return await _context.KnowledgeFiles
            .Where(f => f.AgentId == agentId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<KnowledgeFile?> GetByIdAsync(long id)
    {
        return await _context.KnowledgeFiles.FirstOrDefaultAsync(f => f.KnowledgeFileId == id);
    }

    public async Task<KnowledgeFile> CreateAsync(KnowledgeFile file)
    {
        file.CreatedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;
        _context.KnowledgeFiles.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<KnowledgeFile> UpdateAsync(KnowledgeFile file)
    {
        file.UpdatedAt = DateTime.UtcNow;
        _context.KnowledgeFiles.Update(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task DeleteAsync(long id)
    {
        var file = await GetByIdAsync(id);
        if (file != null)
        {
            _context.KnowledgeFiles.Remove(file);
            await _context.SaveChangesAsync();
        }
    }
}
