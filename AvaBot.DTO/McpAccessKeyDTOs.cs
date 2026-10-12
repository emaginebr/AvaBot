using System.Text.Json.Serialization;

namespace AvaBot.DTO;

// Chave de acesso para assistentes (feature 017). Nunca carrega o hash nem o valor em claro,
// exceto McpAccessKeyCreatedInfo.Key, que so existe na resposta do POST.
public class McpAccessKeyInfo
{
    [JsonPropertyName("mcpAccessKeyId")]
    public long McpAccessKeyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("keyPrefix")]
    public string KeyPrefix { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }
}

public class McpAccessKeyInsertInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class McpAccessKeyCreatedInfo
{
    /// <summary>Valor em claro da chave. Exibido uma unica vez; o servidor guarda so o hash.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("accessKey")]
    public McpAccessKeyInfo AccessKey { get; set; } = new();
}
