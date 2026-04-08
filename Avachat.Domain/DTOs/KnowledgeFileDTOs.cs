using System.Text.Json.Serialization;

namespace Avachat.Domain.DTOs;

public class KnowledgeFileInfo
{
    [JsonPropertyName("knowledgeFileId")]
    public long KnowledgeFileId { get; set; }

    [JsonPropertyName("agentId")]
    public long? AgentId { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("processingStatus")]
    public int ProcessingStatus { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
