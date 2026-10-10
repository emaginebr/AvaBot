using AvaBot.DTO;

namespace AvaBot.Calibration.Report;

// Modelo do relatorio (data-model §4). O resultado de cada mensagem e o proprio
// AgentTestResultInfo devolvido por ChatService.TestMessageAsync.

public enum CalibrationTurnStatus
{
    Concluida,
    Falhou,
    NaoEnviada
}

public class CalibrationRun
{
    public string AgentSlug { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public long DurationMs { get; set; }

    /// <summary>Linhas do cabecalho que descrevem o ambiente (prompt, base, datasets).</summary>
    public List<string> Details { get; set; } = new();

    public List<CalibrationConversation> Conversations { get; set; } = new();

    public IEnumerable<CalibrationTurn> Turns => Conversations.SelectMany(c => c.Turns);
}

public class CalibrationConversation
{
    public string Name { get; set; } = string.Empty;
    public List<CalibrationTurn> Turns { get; set; } = new();
}

public class CalibrationTurn
{
    public int Index { get; set; }
    public string Question { get; set; } = string.Empty;

    /// <summary>Historico enviado junto com a pergunta (mensagens anteriores da conversa).</summary>
    public List<AgentTestMessageInfo> History { get; set; } = new();

    /// <summary>Resultado completo, ou o parcial de uma falha; null quando nada voltou.</summary>
    public AgentTestResultInfo? Result { get; set; }

    public CalibrationTurnStatus Status { get; set; }
    public string? Reason { get; set; }
    public long ClientDurationMs { get; set; }
}
