using System.Text.Json.Serialization;

namespace AvaBot.DTO;

public class AgentInfo
{
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("chatModel")]
    public string ChatModel { get; set; } = string.Empty;

    [JsonPropertyName("hasOpenAIApiKey")]
    public bool HasOpenAIApiKey { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }

    [JsonPropertyName("telegramBotName")]
    public string? TelegramBotName { get; set; }

    [JsonPropertyName("telegramBotToken")]
    public string? TelegramBotToken { get; set; }

    [JsonPropertyName("telegramWebhookSecret")]
    public string? TelegramWebhookSecret { get; set; }

    [JsonPropertyName("whatsappToken")]
    public string? WhatsappToken { get; set; }

    [JsonPropertyName("powerBIEnabled")]
    public bool PowerBIEnabled { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class AgentInsertInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("chatModel")]
    public string ChatModel { get; set; } = "gpt-4o";

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }

    [JsonPropertyName("telegramBotName")]
    public string? TelegramBotName { get; set; }

    [JsonPropertyName("telegramBotToken")]
    public string? TelegramBotToken { get; set; }

    /// <summary>Chave nova em texto claro, aceita apenas na requisição. Nulo/vazio preserva a chave salva.</summary>
    [JsonPropertyName("openAIApiKey")]
    public string? OpenAIApiKey { get; set; }

    /// <summary>Remoção explícita da credencial salva. Incompatível com openAIApiKey preenchida.</summary>
    [JsonPropertyName("removeOpenAIApiKey")]
    public bool RemoveOpenAIApiKey { get; set; }
}

public class TelegramWebhookInfo
{
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }

    [JsonPropertyName("agentSlug")]
    public string AgentSlug { get; set; } = string.Empty;

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("isConfigured")]
    public bool IsConfigured { get; set; }
}

public class AgentChatConfigInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }
}

public class AgentTestQuestionInfo
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    // Mensagens anteriores da conversa (calibracao de conversas, feature 015); opcional.
    [JsonPropertyName("history")]
    public List<AgentTestMessageInfo>? History { get; set; }
}

public class AgentTestResultInfo
{
    [JsonPropertyName("searchQuery")]
    public string SearchQuery { get; set; } = string.Empty;

    [JsonPropertyName("searchResults")]
    public List<string> SearchResults { get; set; } = new();

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<AgentTestMessageInfo> Messages { get; set; } = new();

    [JsonPropertyName("assistantResponse")]
    public string AssistantResponse { get; set; } = string.Empty;

    [JsonPropertyName("powerBIQueries")]
    public List<AgentTestPowerBIQueryInfo> PowerBIQueries { get; set; } = new();

    [JsonPropertyName("chatModel")]
    public string ChatModel { get; set; } = string.Empty;

    [JsonPropertyName("powerBIAvailable")]
    public bool PowerBIAvailable { get; set; }

    [JsonPropertyName("powerBIDatasets")]
    public List<string> PowerBIDatasets { get; set; } = new();

    [JsonPropertyName("maxQueryAttempts")]
    public int? MaxQueryAttempts { get; set; }

    [JsonPropertyName("historyOmittedCount")]
    public int HistoryOmittedCount { get; set; }

    [JsonPropertyName("trace")]
    public AgentTestTraceInfo Trace { get; set; } = new();
}

public class AgentTestMessageInfo
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

// Rastreamento por rodada do modelo no teste de agente (relatorio de calibracao, feature 015).
public class AgentTestTraceInfo
{
    [JsonPropertyName("rounds")]
    public List<AgentTestTraceRoundInfo> Rounds { get; set; } = new();

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class AgentTestTraceRoundInfo
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("messages")]
    public List<AgentTestTraceMessageInfo> Messages { get; set; } = new();

    [JsonPropertyName("toolsOffered")]
    public bool ToolsOffered { get; set; }

    [JsonPropertyName("toolChoiceNone")]
    public bool ToolChoiceNone { get; set; }

    [JsonPropertyName("finishReason")]
    public string FinishReason { get; set; } = string.Empty;

    [JsonPropertyName("responseText")]
    public string? ResponseText { get; set; }

    [JsonPropertyName("toolCalls")]
    public List<AgentTestTraceToolCallInfo> ToolCalls { get; set; } = new();

    [JsonPropertyName("inputTokens")]
    public int? InputTokens { get; set; }

    [JsonPropertyName("outputTokens")]
    public int? OutputTokens { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }
}

public class AgentTestTraceMessageInfo
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("toolCalls")]
    public List<AgentTestTraceToolCallInfo>? ToolCalls { get; set; }

    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; set; }
}

public class AgentTestTraceToolCallInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("argumentsJson")]
    public string ArgumentsJson { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }
}

public class WhatsappQrCodeInfo
{
    [JsonPropertyName("agentSlug")]
    public string AgentSlug { get; set; } = string.Empty;

    [JsonPropertyName("qrCode")]
    public string QrCode { get; set; } = string.Empty;
}

public class WhatsappStatusInfo
{
    [JsonPropertyName("agentSlug")]
    public string AgentSlug { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; set; }
}

public class AgentOpenAIDiagnoseInfo
{
    /// <summary>Chave atual do formulario. Opcional: vazio usa a credencial salva do agente.</summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}

public class AgentOpenAIDiagnoseResultInfo
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class Result<T>
{
    [JsonPropertyName("sucesso")]
    public bool Sucesso { get; set; }

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = string.Empty;

    [JsonPropertyName("erros")]
    public string[] Erros { get; set; } = Array.Empty<string>();

    [JsonPropertyName("dados")]
    public T? Dados { get; set; }

    public static Result<T> Success(T data, string message = "Operacao realizada com sucesso")
    {
        return new Result<T> { Sucesso = true, Mensagem = message, Dados = data };
    }

    public static Result<T> Failure(string message, string[]? errors = null)
    {
        return new Result<T> { Sucesso = false, Mensagem = message, Erros = errors ?? Array.Empty<string>() };
    }
}
