using Avachat.Domain.Enums;

namespace Avachat.Domain.Models;

public class ChatMessage
{
    public long ChatMessageId { get; set; }
    public long? ChatSessionId { get; set; }
    public SenderType SenderType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ChatSession? ChatSession { get; set; }
}
