namespace Avachat.Domain.Models;

public class TelegramChat
{
    public long TelegramChatId { get; set; }
    public long AgentId { get; set; }
    public long ChatSessionId { get; set; }
    public string? TelegramUsername { get; set; }
    public string? TelegramFirstName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agent? Agent { get; set; }
    public ChatSession? ChatSession { get; set; }
}
