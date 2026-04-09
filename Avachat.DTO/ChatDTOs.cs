using System.Text.Json.Serialization;

namespace Avachat.DTO;

public class ChatSessionInfo
{
    [JsonPropertyName("chatSessionId")] public long ChatSessionId { get; set; }
    [JsonPropertyName("agentId")] public long? AgentId { get; set; }
    [JsonPropertyName("userName")] public string? UserName { get; set; }
    [JsonPropertyName("userEmail")] public string? UserEmail { get; set; }
    [JsonPropertyName("userPhone")] public string? UserPhone { get; set; }
    [JsonPropertyName("startedAt")] public DateTime StartedAt { get; set; }
    [JsonPropertyName("endedAt")] public DateTime? EndedAt { get; set; }
    [JsonPropertyName("messageCount")] public int MessageCount { get; set; }
}

public class ChatMessageInfo
{
    [JsonPropertyName("chatMessageId")] public long ChatMessageId { get; set; }
    [JsonPropertyName("chatSessionId")] public long? ChatSessionId { get; set; }
    [JsonPropertyName("senderType")] public int SenderType { get; set; }
    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
}

public class ChatSessionStartInfo
{
    [JsonPropertyName("userName")] public string? UserName { get; set; }
    [JsonPropertyName("userEmail")] public string? UserEmail { get; set; }
    [JsonPropertyName("userPhone")] public string? UserPhone { get; set; }
}

public class PaginatedResult<T>
{
    [JsonPropertyName("items")] public List<T> Items { get; set; } = new();
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("maxPage")] public int MaxPage { get; set; }
}
