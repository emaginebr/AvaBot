namespace Avachat.Domain.Models;

public class Agent
{
    public long AgentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public bool CollectName { get; set; }
    public bool CollectEmail { get; set; }
    public bool CollectPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<KnowledgeFile> KnowledgeFiles { get; set; } = new List<KnowledgeFile>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
