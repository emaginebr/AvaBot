namespace AvaBot.Infra.Interfaces.AppServices;

/// <summary>
/// Registro das idas ao modelo numa chamada de chat: o que foi enviado em cada rodada,
/// o que o modelo decidiu e o que as ferramentas devolveram. Usado pelo teste de agente
/// para o relatorio de calibracao; o chat de producao nao passa trace.
/// </summary>
public class ChatCompletionTrace
{
    public List<ChatTraceRound> Rounds { get; } = new();

    /// <summary>Mensagem da excecao que interrompeu o loop; null quando concluiu.</summary>
    public string? Error { get; set; }
}

public class ChatTraceRound
{
    public int Number { get; set; }

    /// <summary>Conjunto completo enviado nesta rodada, incluindo o system prompt.</summary>
    public List<ChatTraceMessage> Messages { get; set; } = new();

    public bool ToolsOffered { get; set; }

    /// <summary>A rodada foi forcada a responder sem ferramentas porque o limite foi atingido.</summary>
    public bool ToolChoiceNone { get; set; }

    public string FinishReason { get; set; } = string.Empty;

    public string? ResponseText { get; set; }

    public List<ChatTraceToolCall> ToolCalls { get; set; } = new();

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    /// <summary>Tempo da chamada ao modelo, sem a execucao das ferramentas.</summary>
    public long DurationMs { get; set; }
}

public class ChatTraceMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public List<ChatTraceToolCall>? ToolCalls { get; set; }

    public string? ToolCallId { get; set; }
}

public class ChatTraceToolCall
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = string.Empty;

    /// <summary>Texto exato devolvido ao modelo como resultado da ferramenta.</summary>
    public string? Result { get; set; }

    public long? DurationMs { get; set; }
}
