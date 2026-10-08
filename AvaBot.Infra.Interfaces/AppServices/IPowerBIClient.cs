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
    /// <summary>Status HTTP; 0 quando a falha e de transporte e nao houve resposta.</summary>
    public int StatusCode { get; }

    public string? ErrorCode { get; }

    /// <summary>Corpo original integral (segredos conhecidos ja redigidos) para nao perder estrutura nao interpretada.</summary>
    public string? ResponseBody { get; }

    /// <summary>Valor do header Retry-After quando o servico indicou quando voltar.</summary>
    public TimeSpan? RetryAfter { get; }

    public PowerBIApiException(
        int statusCode,
        string? errorCode,
        string message,
        string? responseBody = null,
        TimeSpan? retryAfter = null) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ResponseBody = responseBody;
        RetryAfter = retryAfter;
    }
}

public interface IPowerBIClient
{
    Task<string> GetAccessTokenAsync(PowerBICredentials credentials, CancellationToken cancellationToken = default);
    Task<List<PowerBIWorkspace>> ListWorkspacesWithDatasetsAsync(PowerBICredentials credentials, CancellationToken cancellationToken = default);
    Task<PowerBIQueryResult> ExecuteQueryAsync(PowerBICredentials credentials, string workspaceId, string datasetId, string dax, CancellationToken cancellationToken = default);
    void InvalidateToken(string tenantId, string clientId);
}
