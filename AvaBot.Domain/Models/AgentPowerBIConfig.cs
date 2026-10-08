namespace AvaBot.Domain.Models;

public class AgentPowerBIConfig
{
    public long AgentPowerBIConfigId { get; set; }
    public long AgentId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretEncrypted { get; set; } = string.Empty;
    public string ClientSecretHint { get; set; } = string.Empty;
    public DateTime? LastTestAt { get; set; }
    public bool? LastTestSuccess { get; set; }
    public string? LastTestMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agent? Agent { get; set; }
}
