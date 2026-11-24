using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class WhatsappContactRepository : IWhatsappContactRepository<WhatsappContact>
{
    private readonly AvaBotContext _context;

    public WhatsappContactRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<List<WhatsappContact>> GetAllByAgentAsync(long agentId)
    {
        return await _context.WhatsappContacts
            .Where(c => c.AgentId == agentId)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.WhatsappContactId)
            .ToListAsync();
    }

    public async Task<int> CountByAgentAsync(long agentId)
    {
        return await _context.WhatsappContacts.CountAsync(c => c.AgentId == agentId);
    }

    public async Task<WhatsappContact?> GetByIdAsync(long id, long agentId)
    {
        return await _context.WhatsappContacts
            .FirstOrDefaultAsync(c => c.WhatsappContactId == id && c.AgentId == agentId);
    }

    public async Task<WhatsappContact?> FindBySenderAsync(long agentId, string? phoneMatchKey, string? whatsappId)
    {
        if (string.IsNullOrEmpty(phoneMatchKey) && string.IsNullOrEmpty(whatsappId))
            return null;

        return await _context.WhatsappContacts
            .Where(c => c.AgentId == agentId
                && ((phoneMatchKey != null && c.PhoneMatchKey == phoneMatchKey)
                    || (whatsappId != null && c.WhatsappId == whatsappId)))
            .OrderBy(c => c.WhatsappContactId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(long agentId, string? phoneMatchKey, string? whatsappId, long? excludeId = null)
    {
        if (string.IsNullOrEmpty(phoneMatchKey) && string.IsNullOrEmpty(whatsappId))
            return false;

        return await _context.WhatsappContacts.AnyAsync(c => c.AgentId == agentId
            && (excludeId == null || c.WhatsappContactId != excludeId)
            && ((phoneMatchKey != null && c.PhoneMatchKey == phoneMatchKey)
                || (whatsappId != null && c.WhatsappId == whatsappId)));
    }

    public async Task<WhatsappContact> CreateAsync(WhatsappContact contact)
    {
        contact.CreatedAt = DateTime.UtcNow;
        contact.UpdatedAt = contact.CreatedAt;
        _context.WhatsappContacts.Add(contact);
        await _context.SaveChangesAsync();
        return contact;
    }

    public async Task<WhatsappContact> UpdateAsync(WhatsappContact contact)
    {
        contact.UpdatedAt = DateTime.UtcNow;
        _context.Entry(contact).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return contact;
    }

    public async Task DeleteAsync(WhatsappContact contact)
    {
        // Pedidos aprovados que apontam para o contato perdem o vinculo (FK ClientSetNull feita em codigo).
        var linked = await _context.WhatsappAccessRequests
            .Where(r => r.WhatsappContactId == contact.WhatsappContactId)
            .ToListAsync();
        foreach (var request in linked)
            request.WhatsappContactId = null;

        _context.WhatsappContacts.Remove(contact);
        await _context.SaveChangesAsync();
    }
}
