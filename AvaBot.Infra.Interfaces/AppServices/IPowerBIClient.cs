namespace AvaBot.Infra.Interfaces.AppServices;

public class PowerBICredentials
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class PowerBIDatasetRef
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class PowerBIWorkspace
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PowerBIDatasetRef> Datasets { get; set; } = new();
}

public class PowerBIQueryResult
{
    public List<string> Columns { get; set; } = new();
    public List<List<object?>> Rows { get; set; } = new();
}

public class PowerBIApiException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    public PowerBIApiException(int statusCode, string? errorCode, string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public interface IPowerBIClient
{
    Task<string> GetAccessTokenAsync(PowerBICredentials credentials, CancellationToken cancellationToken = default);
    Task<List<PowerBIWorkspace>> ListWorkspacesWithDatasetsAsync(PowerBICredentials credentials, CancellationToken cancellationToken = default);
    Task<PowerBIQueryResult> ExecuteQueryAsync(PowerBICredentials credentials, string workspaceId, string datasetId, string dax, CancellationToken cancellationToken = default);
    void InvalidateToken(string tenantId, string clientId);
}
