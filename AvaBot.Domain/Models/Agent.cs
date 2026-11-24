using AvaBot.Domain.Enums;

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
    // Controle de acesso do WhatsApp (feature 018). Frases nulas usam o texto padrao.
    public WhatsappAccessMode WhatsappAccessMode { get; set; } = WhatsappAccessMode.All;
    public string? WhatsappOfferMessage { get; set; }
    public string? WhatsappConfirmationMessage { get; set; }
    public string? WhatsappWaitingMessage { get; set; }
    public string? WhatsappDeclinedMessage { get; set; }
    public bool PowerBIEnabled { get; set; }
    // Nulo so entre a migracao de schema e o bootstrap da conta do administrador (feature 016).
    public long? OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? Owner { get; set; }
    public ICollection<KnowledgeFile> KnowledgeFiles { get; set; } = new List<KnowledgeFile>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<PowerBIDataset> PowerBIDatasets { get; set; } = new List<PowerBIDataset>();
    public AgentPowerBIConfig? PowerBIConfig { get; set; }
    public ICollection<WhatsappContact> WhatsappContacts { get; set; } = new List<WhatsappContact>();
    public ICollection<WhatsappAccessRequest> WhatsappAccessRequests { get; set; } = new List<WhatsappAccessRequest>();

    /// <summary>Texto efetivo da frase pronta: o personalizado ou, se nulo, o padrao.</summary>
    public string GetWhatsappMessage(WhatsappAccessMessage kind) => kind switch
    {
        WhatsappAccessMessage.Offer => WhatsappOfferMessage ?? WhatsappAccessDefaults.OfferMessage,
        WhatsappAccessMessage.Confirmation => WhatsappConfirmationMessage ?? WhatsappAccessDefaults.ConfirmationMessage,
        WhatsappAccessMessage.Waiting => WhatsappWaitingMessage ?? WhatsappAccessDefaults.WaitingMessage,
        WhatsappAccessMessage.Declined => WhatsappDeclinedMessage ?? WhatsappAccessDefaults.DeclinedMessage,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string GetDefaultWhatsappMessage(WhatsappAccessMessage kind) => kind switch
    {
        WhatsappAccessMessage.Offer => WhatsappAccessDefaults.OfferMessage,
        WhatsappAccessMessage.Confirmation => WhatsappAccessDefaults.ConfirmationMessage,
        WhatsappAccessMessage.Waiting => WhatsappAccessDefaults.WaitingMessage,
        WhatsappAccessMessage.Declined => WhatsappAccessDefaults.DeclinedMessage,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
