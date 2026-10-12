using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class TelegramChatRepository : ITelegramChatRepository<TelegramChat>
{
    private readonly AvaBotContext _context;

    public TelegramChatRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<TelegramChat?> GetByChatIdAsync(long telegramChatId)
    {
        return await _context.TelegramChats
            .Include(t => t.ChatSession)
            .FirstOrDefaultAsync(t => t.TelegramChatId == telegramChatId);
    }

    public async Task<TelegramChat> CreateAsync(TelegramChat chat)
    {
        chat.CreatedAt = DateTime.UtcNow;
        chat.UpdatedAt = DateTime.UtcNow;
        _context.TelegramChats.Add(chat);
        await _context.SaveChangesAsync();
        return chat;
    }

    public async Task<TelegramChat> UpdateAsync(TelegramChat chat)
    {
        chat.UpdatedAt = DateTime.UtcNow;
        _context.TelegramChats.Update(chat);
        await _context.SaveChangesAsync();
        return chat;
    }

    public async Task<HashSet<long>> GetSessionIdsAsync(IEnumerable<long> chatSessionIds)
    {
        var ids = chatSessionIds.Distinct().ToList();
        if (ids.Count == 0)
            return new HashSet<long>();

        var found = await _context.TelegramChats
            .Where(t => ids.Contains(t.ChatSessionId))
            .Select(t => t.ChatSessionId)
            .ToListAsync();

        return found.ToHashSet();
    }
}
