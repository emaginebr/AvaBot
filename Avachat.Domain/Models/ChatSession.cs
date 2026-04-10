namespace Avachat.Domain.Models;

public class ChatSession
{
    public long ChatSessionId { get; set; }
    public long? AgentId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public string ResumeToken { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public Agent? Agent { get; set; }
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
