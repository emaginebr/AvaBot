using Microsoft.EntityFrameworkCore;
using Avachat.Domain.Models;
using Avachat.Infra.Context;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Infra.Repository;

public class TelegramChatRepository : ITelegramChatRepository<TelegramChat>
{
    private readonly AvachatContext _context;

    public TelegramChatRepository(AvachatContext context)
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
}
