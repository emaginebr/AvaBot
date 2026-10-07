using AvaBot.Domain.Enums;

namespace AvaBot.Domain.Models;

public class PowerBIDataset
{
    public long PowerBIDatasetId { get; set; }
    public long AgentId { get; set; }
    public string WorkspaceId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ToolKey { get; set; } = string.Empty;
    public string? SchemaJson { get; set; }
    public PowerBISchemaStatus SchemaStatus { get; set; } = PowerBISchemaStatus.NotGenerated;
    public DateTime? SchemaGeneratedAt { get; set; }
    public string? SchemaError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agent? Agent { get; set; }

    public bool IsUsable => !string.IsNullOrWhiteSpace(SchemaJson);
}
