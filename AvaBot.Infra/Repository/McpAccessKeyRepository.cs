using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class McpAccessKeyRepository : IMcpAccessKeyRepository<McpAccessKey>
{
    private readonly AvaBotContext _context;

    public McpAccessKeyRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<McpAccessKey?> GetByIdAsync(long id, long userId)
    {
        return await _context.McpAccessKeys
            .FirstOrDefaultAsync(k => k.McpAccessKeyId == id && k.UserId == userId);
    }

    public async Task<McpAccessKey?> GetByHashAsync(string keyHash)
    {
        return await _context.McpAccessKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);
    }

    public async Task<List<McpAccessKey>> GetAllByUserAsync(long userId)
    {
        return await _context.McpAccessKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ThenByDescending(k => k.McpAccessKeyId)
            .ToListAsync();
    }

    public async Task<int> CountActiveByUserAsync(long userId)
    {
        return await _context.McpAccessKeys.CountAsync(k => k.UserId == userId && k.Status == 1);
    }

    public async Task<McpAccessKey> CreateAsync(McpAccessKey key)
    {
        key.CreatedAt = DateTime.UtcNow;
        _context.McpAccessKeys.Add(key);
        await _context.SaveChangesAsync();
        return key;
    }

    public async Task<McpAccessKey> UpdateAsync(McpAccessKey key)
    {
        // Marca so a chave: o usuario dono (carregado em GetByHashAsync) nao e tocado.
        _context.Entry(key).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return key;
    }
}
