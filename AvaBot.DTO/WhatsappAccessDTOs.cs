using System.Text.Json.Serialization;

namespace AvaBot.DTO;

// Controle de acesso do WhatsApp (feature 018). Nenhum DTO carrega o WhatsappToken.

public class WhatsappAccessConfigInfo
{
    /// <summary>1 = Todos, 2 = Somente contatos autorizados.</summary>
    [JsonPropertyName("mode")]
    public int Mode { get; set; }

    [JsonPropertyName("offerMessage")]
    public WhatsappAccessMessageInfo OfferMessage { get; set; } = new();

    [JsonPropertyName("confirmationMessage")]
    public WhatsappAccessMessageInfo ConfirmationMessage { get; set; } = new();

    [JsonPropertyName("waitingMessage")]
    public WhatsappAccessMessageInfo WaitingMessage { get; set; } = new();

    [JsonPropertyName("declinedMessage")]
    public WhatsappAccessMessageInfo DeclinedMessage { get; set; } = new();

    [JsonPropertyName("contactCount")]
    public int ContactCount { get; set; }

    [JsonPropertyName("pendingRequestCount")]
    public int PendingRequestCount { get; set; }
}

public class WhatsappAccessMessageInfo
{
    /// <summary>Texto efetivo: o personalizado ou, se nao houver, o padrao.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("defaultText")]
    public string DefaultText { get; set; } = string.Empty;
}

public class WhatsappAccessConfigUpdateInfo
{
    [JsonPropertyName("mode")]
    public int Mode { get; set; }

    // null = restaurar o texto padrao; string vazia = erro de validacao.
    [JsonPropertyName("offerMessage")]
    public string? OfferMessage { get; set; }

    [JsonPropertyName("confirmationMessage")]
    public string? ConfirmationMessage { get; set; }

    [JsonPropertyName("waitingMessage")]
    public string? WaitingMessage { get; set; }

    [JsonPropertyName("declinedMessage")]
    public string? DeclinedMessage { get; set; }
}

public class WhatsappContactInfo
{
    [JsonPropertyName("whatsappContactId")]
    public long WhatsappContactId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Como digitado; nulo se o contato veio de aprovacao sem telefone conhecido.</summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("phoneNormalized")]
    public string? PhoneNormalized { get; set; }

    [JsonPropertyName("whatsappId")]
    public string? WhatsappId { get; set; }

    /// <summary>1 = manual, 2 = aprovacao.</summary>
    [JsonPropertyName("source")]
    public int Source { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class WhatsappContactInsertInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;
}

public class WhatsappAccessRequestInfo
{
    [JsonPropertyName("whatsappAccessRequestId")]
    public long WhatsappAccessRequestId { get; set; }

    [JsonPropertyName("whatsappId")]
    public string WhatsappId { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("profileName")]
    public string? ProfileName { get; set; }

    /// <summary>1 = pendente, 2 = aprovada, 3 = rejeitada.</summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }

    [JsonPropertyName("decidedAt")]
    public DateTime? DecidedAt { get; set; }

    [JsonPropertyName("whatsappContactId")]
    public long? WhatsappContactId { get; set; }
}

public class WhatsappAccessRequestApproveInfo
{
    /// <summary>Opcional; padrao: nome de perfil, telefone ou JID.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
