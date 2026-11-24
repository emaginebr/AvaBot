using AvaBot.Domain.Enums;

namespace AvaBot.Domain.Models;

/// <summary>Contato autorizado a usar o agente pelo WhatsApp quando o modo e restrito (feature 018).</summary>
public class WhatsappContact
{
    public long WhatsappContactId { get; set; }
    public long AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Telefone como digitado (exibicao). Nulo quando veio de aprovacao sem telefone conhecido.</summary>
    public string? Phone { get; set; }
    public string? PhoneNormalized { get; set; }
    public string? PhoneMatchKey { get; set; }
    /// <summary>JID do WhatsApp ("...@c.us" ou "...@lid").</summary>
    public string? WhatsappId { get; set; }
    public WhatsappContactSource Source { get; set; } = WhatsappContactSource.Manual;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agent? Agent { get; set; }

    /// <summary>Atualiza telefone, forma normalizada e chave de comparacao.</summary>
    public void SetPhone(string? phone, string? normalized)
    {
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        PhoneNormalized = normalized;
        PhoneMatchKey = WhatsappPhone.MatchKey(normalized);
    }
}
