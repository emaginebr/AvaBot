using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AvaBot.DTO;

// DTOs do servidor MCP (feature 017). Nenhum tipo deste arquivo tem campo de segredo:
// chave de API, Directory ID, App ID, segredo do Power BI, tokens do Telegram e do WhatsApp
// aparecem apenas como indicadores "configurado / nao configurado" (FR-009).

public class McpIdentityInfo
{
    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("accessKeyName")]
    public string AccessKeyName { get; set; } = string.Empty;
}

public class AgentMcpSummaryInfo
{
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("chatModel")]
    public string ChatModel { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("hasOpenAIApiKey")]
    public bool HasOpenAIApiKey { get; set; }

    [JsonPropertyName("powerBIEnabled")]
    public bool PowerBIEnabled { get; set; }

    [JsonPropertyName("telegramBotName")]
    public string? TelegramBotName { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class AgentMcpInfo : AgentMcpSummaryInfo
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }

    [JsonPropertyName("telegramBotConfigured")]
    public bool TelegramBotConfigured { get; set; }

    [JsonPropertyName("whatsappConfigured")]
    public bool WhatsappConfigured { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class AgentMcpCreateInfo
{
    [JsonPropertyName("name")]
    [Description("Nome do agente (obrigatorio, ate 260 caracteres).")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("systemPrompt")]
    [Description("Prompt de sistema que define o comportamento do agente (obrigatorio).")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [Description("Descricao curta exibida no painel e no chat.")]
    public string? Description { get; set; }

    [JsonPropertyName("chatModel")]
    [Description("Modelo de chat (padrao gpt-4o).")]
    public string? ChatModel { get; set; }

    [JsonPropertyName("collectName")]
    [Description("Pedir o nome do visitante antes da conversa.")]
    public bool? CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    [Description("Pedir o e-mail do visitante antes da conversa.")]
    public bool? CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    [Description("Pedir o telefone do visitante antes da conversa.")]
    public bool? CollectPhone { get; set; }

    [JsonPropertyName("telegramBotName")]
    [Description("Nome de exibicao do bot do Telegram (nao e o token).")]
    public string? TelegramBotName { get; set; }
}

/// <summary>Atualizacao parcial: campo nulo mantem o valor atual. Nunca inclui credenciais.</summary>
public class AgentProfileUpdateInfo
{
    [JsonPropertyName("name")]
    [Description("Novo nome do agente; renomear regenera o endereco (slug).")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    [Description("Nova descricao.")]
    public string? Description { get; set; }

    [JsonPropertyName("systemPrompt")]
    [Description("Novo prompt de sistema.")]
    public string? SystemPrompt { get; set; }

    [JsonPropertyName("chatModel")]
    [Description("Novo modelo de chat.")]
    public string? ChatModel { get; set; }

    [JsonPropertyName("collectName")]
    [Description("Pedir o nome do visitante.")]
    public bool? CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    [Description("Pedir o e-mail do visitante.")]
    public bool? CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    [Description("Pedir o telefone do visitante.")]
    public bool? CollectPhone { get; set; }

    [JsonPropertyName("telegramBotName")]
    [Description("Nome de exibicao do bot do Telegram (nao e o token).")]
    public string? TelegramBotName { get; set; }
}

public class ChatSessionMcpInfo
{
    [JsonPropertyName("chatSessionId")]
    public long ChatSessionId { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("endedAt")]
    public DateTime? EndedAt { get; set; }

    /// <summary>web ou telegram. Site, widget e WhatsApp sao indistinguiveis hoje e aparecem como web.</summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "web";

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("userEmail")]
    public string? UserEmail { get; set; }

    [JsonPropertyName("userPhone")]
    public string? UserPhone { get; set; }

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }
}

public class ChatMessageMcpInfo
{
    [JsonPropertyName("chatMessageId")]
    public long ChatMessageId { get; set; }

    /// <summary>visitante ou agente.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class PowerBIMcpInfo
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("credentialConfigured")]
    public bool CredentialConfigured { get; set; }

    [JsonPropertyName("lastTestAt")]
    public DateTime? LastTestAt { get; set; }

    [JsonPropertyName("lastTestSuccess")]
    public bool? LastTestSuccess { get; set; }

    [JsonPropertyName("lastTestMessage")]
    public string? LastTestMessage { get; set; }

    [JsonPropertyName("datasets")]
    public List<PowerBIDatasetInfo> Datasets { get; set; } = new();
}

public class TelegramMcpInfo
{
    [JsonPropertyName("botName")]
    public string? BotName { get; set; }

    [JsonPropertyName("botConfigured")]
    public bool BotConfigured { get; set; }

    [JsonPropertyName("webhookRegistered")]
    public bool WebhookRegistered { get; set; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; set; }
}

public class WhatsappMcpInfo
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isConnected")]
    public bool IsConnected { get; set; }
}

public class KnowledgeDocumentMcpInsertInfo
{
    [JsonPropertyName("fileName")]
    [Description("Nome do documento; .md e acrescentado se faltar.")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    [Description("Conteudo do documento em texto (Markdown), ate 10 MB.")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>Resposta das ferramentas que nao devolvem entidade (desvincular dataset, remover documento).</summary>
public class McpMessageInfo
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
