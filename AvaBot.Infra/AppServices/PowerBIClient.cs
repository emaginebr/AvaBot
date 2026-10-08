using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Infra.AppServices;

public class PowerBIClient : IPowerBIClient
{
    private const string Scope = "https://analysis.windows.net/powerbi/api/.default";
    private const string TokenCachePrefix = "pbi-token:";
    private const int TokenSafetyWindowSeconds = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly string _apiBaseUrl;
    private readonly string _authorityBaseUrl;
    private readonly ILogger<PowerBIClient> _logger;

    public PowerBIClient(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<PowerBIClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _apiBaseUrl = (configuration["PowerBI:ApiBaseUrl"] ?? "https://api.powerbi.com/v1.0/myorg").TrimEnd('/');
        _authorityBaseUrl = (configuration["PowerBI:AuthorityBaseUrl"] ?? "https://login.microsoftonline.com").TrimEnd('/');
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(PowerBICredentials credentials, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(credentials.TenantId, credentials.ClientId);

        if (_cache.TryGetValue(cacheKey, out object? cached) && cached is string cachedToken && cachedToken.Length > 0)
            return cachedToken;

        var client = _httpClientFactory.CreateClient("EntraId");
        var tokenUrl = $"{_authorityBaseUrl}/{credentials.TenantId}/oauth2/v2.0/token";

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = credentials.ClientId,
            ["client_secret"] = credentials.ClientSecret,
            ["scope"] = Scope
        });

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(tokenUrl, form, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PowerBIApiException(0, "network_error", $"Falha de rede ao autenticar no Entra ID: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string? description = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                description = doc.RootElement.TryGetProperty("error_description", out var d) ? d.GetString() : null;
            }
            catch (JsonException)
            {
                // corpo de erro nao e JSON
            }

            _logger.LogWarning("Entra ID token request failed with status {StatusCode} for tenant {TenantId}",
                (int)response.StatusCode, credentials.TenantId);

            throw new PowerBIApiException((int)response.StatusCode, "authentication_failed",
                description ?? $"Entra ID retornou {(int)response.StatusCode} sem mensagem reconhecivel.",
                string.IsNullOrWhiteSpace(body) ? null : Redact(body, credentials, null));
        }

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var token = root.GetProperty("access_token").GetString()
            ?? throw new PowerBIApiException((int)response.StatusCode, "authentication_failed", "Entra ID nao retornou access token");
        var expiresIn = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) ? seconds : 3599;

        var lifetime = TimeSpan.FromSeconds(Math.Max(60, expiresIn - TokenSafetyWindowSeconds));
        _cache.Set(cacheKey, token, lifetime);

        _logger.LogInformation("Power BI access token obtido para tenant {TenantId} com validade de {Lifetime:F0}s",
            credentials.TenantId, lifetime.TotalSeconds);

        return token;
    }

    public void InvalidateToken(string tenantId, string clientId)
    {
        _cache.Remove(BuildCacheKey(tenantId, clientId));
    }

    public async Task<List<PowerBIWorkspace>> ListWorkspacesWithDatasetsAsync(PowerBICredentials credentials, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(credentials, cancellationToken);
        var client = _httpClientFactory.CreateClient("PowerBI");

        var workspaces = await GetAsync<List<WorkspaceDto>>(client, credentials, token, "/groups", cancellationToken)
            ?? new List<WorkspaceDto>();

        var result = new List<PowerBIWorkspace>();

        foreach (var workspace in workspaces)
        {
            var datasets = await GetAsync<List<DatasetDto>>(client, credentials, token, $"/groups/{workspace.Id}/datasets", cancellationToken)
                ?? new List<DatasetDto>();

            if (datasets.Count == 0) continue;

            result.Add(new PowerBIWorkspace
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Datasets = datasets.Select(d => new PowerBIDatasetRef { Id = d.Id, Name = d.Name }).ToList()
            });
        }

        return result;
    }

    public async Task<PowerBIQueryResult> ExecuteQueryAsync(PowerBICredentials credentials, string workspaceId, string datasetId, string dax, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(credentials, cancellationToken);
        var client = _httpClientFactory.CreateClient("PowerBI");

        var payload = new
        {
            queries = new[] { new { query = dax } },
            serializerSettings = new { includeNulls = true }
        };

        var url = $"{_apiBaseUrl}/groups/{workspaceId}/datasets/{datasetId}/executeQueries";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PowerBIApiException(0, "network_error", $"Falha de rede ao consultar o Power BI: {ex.Message}");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw BuildApiException(response, body, credentials, token);

            // D5: um HTTP 200 cujo corpo traz erro estruturado nao e resultado vazio bem-sucedido.
            if (BodyReportsError(body))
                throw BuildApiException(response, body, credentials, token);

            return ParseQueryResult(body);
        }
    }

    private async Task<T?> GetAsync<T>(HttpClient client, PowerBICredentials credentials, string token, string relativePath, CancellationToken cancellationToken)
    {
        var url = $"{_apiBaseUrl}{relativePath}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new PowerBIApiException(0, "network_error", $"Falha de rede ao consultar o Power BI: {ex.Message}");
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw BuildApiException(response, body, credentials, token);

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("value", out var valueElement))
                return default;

            return valueElement.Deserialize<T>(JsonOptions);
        }
    }

    private PowerBIApiException BuildApiException(HttpResponseMessage response, string body, PowerBICredentials credentials, string? accessToken)
    {
        var status = (int)response.StatusCode;
        string? errorCode = null;
        var diagnosis = "(sem mensagem reconhecivel na resposta)";
        var recognized = false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var rootError)
                && rootError.ValueKind == JsonValueKind.Object)
            {
                errorCode = ReadString(rootError, "code");
                diagnosis = BuildDiagnosis(rootError);
                recognized = !string.IsNullOrWhiteSpace(diagnosis);
            }
        }
        catch (JsonException)
        {
            // corpo de erro nao e JSON: preservamos o texto integral abaixo
        }

        _logger.LogWarning("Power BI API retornou status {StatusCode} com codigo {ErrorCode}",
            status, errorCode ?? "(nenhum)");

        // O corpo original so e exposto como diagnostico; nao vai para o log operacional (plan:1).
        var responseBody = string.IsNullOrWhiteSpace(body)
            ? null
            : Redact(body, credentials, accessToken);

        var message = recognized
            ? diagnosis
            : $"Power BI retornou status {status} sem mensagem de erro reconhecivel.";

        return new PowerBIApiException(status, errorCode, message, responseBody, ReadRetryAfter(response));
    }

    /// <summary>
    /// Localiza erro estruturado no corpo de uma resposta HTTP bem-sucedida: raiz, results[].error
    /// e results[].tables[].error (D5). Um 200 com erro nao e sucesso.
    /// </summary>
    private static bool BodyReportsError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (doc.RootElement.TryGetProperty("error", out var rootError)
                && rootError.ValueKind == JsonValueKind.Object)
                return true;

            if (doc.RootElement.TryGetProperty("results", out var results)
                && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var result in results.EnumerateArray())
                {
                    if (result.ValueKind != JsonValueKind.Object) continue;

                    if (result.TryGetProperty("error", out var resultError)
                        && resultError.ValueKind == JsonValueKind.Object)
                        return true;

                    if (result.TryGetProperty("tables", out var tables)
                        && tables.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var table in tables.EnumerateArray())
                        {
                            if (table.ValueKind == JsonValueKind.Object
                                && table.TryGetProperty("error", out var tableError)
                                && tableError.ValueKind == JsonValueKind.Object)
                                return true;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // nao e JSON; quem decide o que fazer e o chamador
        }

        return false;
    }

    private static string BuildDiagnosis(JsonElement error)
    {
        var parts = new List<string>();

        var main = ReadString(error, "message");
        if (!string.IsNullOrWhiteSpace(main))
            parts.Add(main!);

        // Detalhes aninhados nao substituem a mensagem principal (D1): vem tudo, em ordem.
        CollectDetailTexts(error, parts);

        // O Power BI marca identificadores com <oii>...</oii>; sem as tags o modelo le o nome exato.
        return string.Join(" ", parts.Distinct(StringComparer.Ordinal))
            .Replace("<oii>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</oii>", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static void CollectDetailTexts(JsonElement node, List<string> found)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in node.EnumerateArray())
                    CollectDetailTexts(item, found);
                break;

            case JsonValueKind.Object:
                foreach (var property in node.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var text = property.Value.GetString();

                        // "value" e o texto dentro de details[].details.detail.value; "message" e o
                        // formato comum. "code" fica de fora porque nao e prosa legivel.
                        if (!string.IsNullOrWhiteSpace(text)
                            && (property.Name.Equals("message", StringComparison.OrdinalIgnoreCase)
                                || property.Name.Equals("value", StringComparison.OrdinalIgnoreCase))
                            && !found.Contains(text!, StringComparer.Ordinal))
                        {
                            found.Add(text!);
                        }

                        continue;
                    }

                    CollectDetailTexts(property.Value, found);
                }
                break;
        }
    }

    private static string? ReadString(JsonElement node, string property) =>
        node.ValueKind == JsonValueKind.Object
        && node.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Retry-After", out var values))
            return null;

        var raw = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(Math.Max(0, seconds));
        }

        if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var retryDate))
        {
            var wait = retryDate - DateTime.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }

    private static string Redact(string text, PowerBICredentials credentials, string? accessToken)
    {
        var redacted = text;

        if (!string.IsNullOrEmpty(credentials.ClientSecret))
            redacted = redacted.Replace(credentials.ClientSecret, "[redacted]");

        if (!string.IsNullOrEmpty(accessToken))
            redacted = redacted.Replace(accessToken!, "[redacted]");

        return redacted;
    }

    private static PowerBIQueryResult ParseQueryResult(string body)
    {
        var result = new PowerBIQueryResult();

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("results", out var queries)
            || queries.ValueKind != JsonValueKind.Array
            || queries.GetArrayLength() == 0)
        {
            return result;
        }

        var firstQuery = queries[0];
        if (!firstQuery.TryGetProperty("tables", out var tables)
            || tables.ValueKind != JsonValueKind.Array
            || tables.GetArrayLength() == 0)
        {
            return result;
        }

        var rows = tables[0].TryGetProperty("rows", out var r) ? r : default;
        if (rows.ValueKind != JsonValueKind.Array)
            return result;

        var columns = new List<string>();

        foreach (var row in rows.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object) continue;

            foreach (var property in row.EnumerateObject())
            {
                if (!columns.Contains(property.Name))
                    columns.Add(property.Name);
            }

            // Coluna ausente na linha entra como null na ordem ja estabelecida.
            var padded = new List<object?>(columns.Count);
            foreach (var column in columns)
            {
                padded.Add(row.TryGetProperty(column, out var cell) ? ReadValue(cell) : null);
            }

            result.Rows.Add(padded);
        }

        result.Columns.AddRange(columns);
        return result;
    }

    private static object? ReadValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        _ => element.GetRawText()
    };

    private static string BuildCacheKey(string tenantId, string clientId) => $"{TokenCachePrefix}{tenantId}:{clientId}";

    private class WorkspaceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class DatasetDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
