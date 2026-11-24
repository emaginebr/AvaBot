namespace AvaBot.Domain.Models;

/// <summary>
/// Estado do fluxo de solicitacao por (agente, remetente): ultima frase automatica e oferta em aberto.
/// Usado para o limite de 1 frase por hora e para reconhecer a resposta a oferta em ate 24 h (feature 018).
/// </summary>
public class WhatsappSenderState
{
    public long WhatsappSenderStateId { get; set; }
    public long AgentId { get; set; }
    public string WhatsappId { get; set; } = string.Empty;
    public DateTime? LastAutoReplyAt { get; set; }
    public DateTime? OfferSentAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agent? Agent { get; set; }
}
