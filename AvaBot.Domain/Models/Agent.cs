namespace AvaBot.Domain.Models;

public class Agent
{
    public long AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public string? OpenAIApiKeyEncrypted { get; set; }
    public int Status { get; set; } = 1;
    public bool CollectName { get; set; }
    public bool CollectEmail { get; set; }
    public bool CollectPhone { get; set; }
    public string? TelegramBotName { get; set; }
    public string? TelegramBotToken { get; set; }
    public string? TelegramWebhookSecret { get; set; }
    public string? WhatsappToken { get; set; }
    public bool PowerBIEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<KnowledgeFile> KnowledgeFiles { get; set; } = new List<KnowledgeFile>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<PowerBIDataset> PowerBIDatasets { get; set; } = new List<PowerBIDataset>();
    public AgentPowerBIConfig? PowerBIConfig { get; set; }
}
