using System.Text.Json.Serialization;

namespace AvaBot.DTO;

public class PowerBIConfigInfo
{
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("isConfigured")]
    public bool IsConfigured { get; set; }

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("hasClientSecret")]
    public bool HasClientSecret { get; set; }

    [JsonPropertyName("clientSecretMasked")]
    public string? ClientSecretMasked { get; set; }

    [JsonPropertyName("lastTestAt")]
    public DateTime? LastTestAt { get; set; }

    [JsonPropertyName("lastTestSuccess")]
    public bool? LastTestSuccess { get; set; }

    [JsonPropertyName("lastTestMessage")]
    public string? LastTestMessage { get; set; }
}

public class PowerBIConfigUpdateInfo
{
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }
}

public class PowerBIEnabledUpdateInfo
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

public class PowerBIConnectionTestStepInfo
{
    [JsonPropertyName("step")]
    public string Step { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class PowerBIConnectionTestInfo
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("steps")]
    public List<PowerBIConnectionTestStepInfo> Steps { get; set; } = new();
}

public class PowerBIWorkspaceDatasetInfo
{
    [JsonPropertyName("datasetId")]
    public string DatasetId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class PowerBIWorkspaceInfo
{
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("datasets")]
    public List<PowerBIWorkspaceDatasetInfo> Datasets { get; set; } = new();
}

public class PowerBIDatasetInfo
{
    [JsonPropertyName("powerBIDatasetId")]
    public long PowerBIDatasetId { get; set; }

    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("datasetId")]
    public string DatasetId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("toolKey")]
    public string ToolKey { get; set; } = string.Empty;

    [JsonPropertyName("schemaStatus")]
    public int SchemaStatus { get; set; }

    [JsonPropertyName("schemaGeneratedAt")]
    public DateTime? SchemaGeneratedAt { get; set; }

    [JsonPropertyName("schemaError")]
    public string? SchemaError { get; set; }

    [JsonPropertyName("tableCount")]
    public int TableCount { get; set; }

    [JsonPropertyName("columnCount")]
    public int ColumnCount { get; set; }

    [JsonPropertyName("measureCount")]
    public int MeasureCount { get; set; }
}

public class PowerBIDatasetInsertInfo
{
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    [JsonPropertyName("datasetId")]
    public string DatasetId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class PowerBISchemaColumnInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }
}

public class PowerBISchemaMeasureInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }
}

public class PowerBISchemaTableInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }

    [JsonPropertyName("columns")]
    public List<PowerBISchemaColumnInfo> Columns { get; set; } = new();

    [JsonPropertyName("measures")]
    public List<PowerBISchemaMeasureInfo> Measures { get; set; } = new();
}

public class PowerBIDatasetSchemaInfo
{
    [JsonPropertyName("powerBIDatasetId")]
    public long PowerBIDatasetId { get; set; }

    [JsonPropertyName("schemaStatus")]
    public int SchemaStatus { get; set; }

    [JsonPropertyName("schemaGeneratedAt")]
    public DateTime? SchemaGeneratedAt { get; set; }

    [JsonPropertyName("schemaError")]
    public string? SchemaError { get; set; }

    [JsonPropertyName("tables")]
    public List<PowerBISchemaTableInfo> Tables { get; set; } = new();
}

public class PowerBISchemaDescriptionItemInfo
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }
}

public class PowerBISchemaDescriptionUpdateInfo
{
    [JsonPropertyName("items")]
    public List<PowerBISchemaDescriptionItemInfo> Items { get; set; } = new();
}

public class PowerBIQueryLogInfo
{
    [JsonPropertyName("powerBIQueryLogId")]
    public long PowerBIQueryLogId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("chatSessionId")]
    public long? ChatSessionId { get; set; }

    [JsonPropertyName("datasetName")]
    public string? DatasetName { get; set; }

    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("userQuestion")]
    public string? UserQuestion { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; set; }

    [JsonPropertyName("rowCount")]
    public int? RowCount { get; set; }

    [JsonPropertyName("truncated")]
    public bool Truncated { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public class PowerBIQueryLogPageInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("items")]
    public List<PowerBIQueryLogInfo> Items { get; set; } = new();
}

public class AgentTestPowerBIQueryInfo
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("datasetName")]
    public string? DatasetName { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; set; }

    [JsonPropertyName("rowCount")]
    public int? RowCount { get; set; }

    [JsonPropertyName("truncated")]
    public bool Truncated { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("resultPreview")]
    public string? ResultPreview { get; set; }
}
