using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class WhatsappAccessRequestRepository : IWhatsappAccessRequestRepository<WhatsappAccessRequest>
{
    private readonly AvaBotContext _context;

    public WhatsappAccessRequestRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<List<WhatsappAccessRequest>> GetByAgentAsync(long agentId, int? status)
    {
        var query = _context.WhatsappAccessRequests.Where(r => r.AgentId == agentId);
        if (status.HasValue)
        {
            var typed = (WhatsappAccessRequestStatus)status.Value;
            query = query.Where(r => r.Status == typed);
        }

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .ThenByDescending(r => r.WhatsappAccessRequestId)
            .ToListAsync();
    }

    public async Task<int> CountPendingAsync(long agentId)
    {
        return await _context.WhatsappAccessRequests
            .CountAsync(r => r.AgentId == agentId && r.Status == WhatsappAccessRequestStatus.Pending);
    }

    public async Task<WhatsappAccessRequest?> GetByIdAsync(long id, long agentId)
    {
        return await _context.WhatsappAccessRequests
            .FirstOrDefaultAsync(r => r.WhatsappAccessRequestId == id && r.AgentId == agentId);
    }

    public async Task<WhatsappAccessRequest?> GetCurrentForSenderAsync(long agentId, string whatsappId, string? phoneMatchKey)
    {
        return await _context.WhatsappAccessRequests
            .Where(r => r.AgentId == agentId
                && (r.WhatsappId == whatsappId || (phoneMatchKey != null && r.PhoneMatchKey == phoneMatchKey)))
            .OrderByDescending(r => r.RequestedAt)
            .ThenByDescending(r => r.WhatsappAccessRequestId)
            .FirstOrDefaultAsync();
    }

    public async Task<WhatsappAccessRequest?> GetPendingByPhoneAsync(long agentId, string phoneMatchKey)
    {
        return await _context.WhatsappAccessRequests
            .Where(r => r.AgentId == agentId
                && r.Status == WhatsappAccessRequestStatus.Pending
                && r.PhoneMatchKey == phoneMatchKey)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<WhatsappAccessRequest> CreateAsync(WhatsappAccessRequest request)
    {
        _context.WhatsappAccessRequests.Add(request);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Nao deixa a entidade presa no change tracker do escopo (ex.: dois "sim" simultaneos).
            _context.Entry(request).State = EntityState.Detached;
            throw;
        }
        return request;
    }

    public async Task<WhatsappAccessRequest> UpdateAsync(WhatsappAccessRequest request)
    {
        _context.Entry(request).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return request;
    }
}
