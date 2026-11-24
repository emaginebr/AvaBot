using Microsoft.EntityFrameworkCore;
using AvaBot.Domain.Models;
using AvaBot.Infra.Context;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Infra.Repository;

public class WhatsappSenderStateRepository : IWhatsappSenderStateRepository<WhatsappSenderState>
{
    private readonly AvaBotContext _context;

    public WhatsappSenderStateRepository(AvaBotContext context)
    {
        _context = context;
    }

    public async Task<WhatsappSenderState?> GetAsync(long agentId, string whatsappId)
    {
        return await _context.WhatsappSenderStates
            .FirstOrDefaultAsync(s => s.AgentId == agentId && s.WhatsappId == whatsappId);
    }

    public async Task<WhatsappSenderState> UpsertAsync(WhatsappSenderState state)
    {
        state.UpdatedAt = DateTime.UtcNow;

        if (state.WhatsappSenderStateId != 0)
        {
            _context.Entry(state).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return state;
        }

        _context.WhatsappSenderStates.Add(state);
        try
        {
            await _context.SaveChangesAsync();
            return state;
        }
        catch (DbUpdateException)
        {
            // Outra mensagem do mesmo remetente criou a linha antes: atualiza a existente.
            _context.Entry(state).State = EntityState.Detached;
            var existing = await GetAsync(state.AgentId, state.WhatsappId);
            if (existing == null)
                throw;

            existing.LastAutoReplyAt = state.LastAutoReplyAt;
            existing.OfferSentAt = state.OfferSentAt;
            existing.UpdatedAt = state.UpdatedAt;
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
