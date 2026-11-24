using AvaBot.Domain.Enums;

namespace AvaBot.Domain.Models;

/// <summary>Pedido de acesso feito pelo WhatsApp por um remetente nao autorizado (feature 018).</summary>
public class WhatsappAccessRequest
{
    public long WhatsappAccessRequestId { get; set; }
    public long AgentId { get; set; }
    public string WhatsappId { get; set; } = string.Empty;
    public string? PhoneNormalized { get; set; }
    public string? PhoneMatchKey { get; set; }
    public string? ProfileName { get; set; }
    public WhatsappAccessRequestStatus Status { get; set; } = WhatsappAccessRequestStatus.Pending;
    public DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public long? DecidedByUserId { get; set; }
    public long? WhatsappContactId { get; set; }

    public Agent? Agent { get; set; }
    public User? DecidedByUser { get; set; }
    public WhatsappContact? WhatsappContact { get; set; }
}
