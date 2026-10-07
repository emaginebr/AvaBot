using AvaBot.Domain.Enums;

namespace AvaBot.Domain.Models;

public class PowerBIQueryLog
{
    public long PowerBIQueryLogId { get; set; }
    public long AgentId { get; set; }
    public long? ChatSessionId { get; set; }
    public long? PowerBIDatasetId { get; set; }
    public string? DatasetName { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string? UserQuestion { get; set; }
    public string? Query { get; set; }
    public int DurationMs { get; set; }
    public int? RowCount { get; set; }
    public bool Truncated { get; set; }
    public PowerBIQueryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    public Agent? Agent { get; set; }
    public ChatSession? ChatSession { get; set; }
    public PowerBIDataset? PowerBIDataset { get; set; }
}
