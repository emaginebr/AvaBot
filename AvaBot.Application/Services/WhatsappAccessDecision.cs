namespace AvaBot.Application.Services;

public enum WhatsappAccessDecisionKind
{
    Allow,
    Reply,
    Ignore
}

/// <summary>Resultado do controle de acesso para uma mensagem recebida (feature 018).</summary>
public sealed class WhatsappAccessDecision
{
    public WhatsappAccessDecisionKind Kind { get; private init; }
    /// <summary>Texto a enviar quando Kind = Reply.</summary>
    public string? Text { get; private init; }
    /// <summary>Etapa (Reply) ou motivo (Ignore), para o log.</summary>
    public string Reason { get; private init; } = string.Empty;

    public static WhatsappAccessDecision Allow() => new() { Kind = WhatsappAccessDecisionKind.Allow, Reason = "Allow" };
    public static WhatsappAccessDecision Reply(string text, string stage) => new() { Kind = WhatsappAccessDecisionKind.Reply, Text = text, Reason = stage };
    public static WhatsappAccessDecision Ignore(string reason) => new() { Kind = WhatsappAccessDecisionKind.Ignore, Reason = reason };
}
