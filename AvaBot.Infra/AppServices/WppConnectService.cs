using System.Text;
using System.Text.Json;
using AvaBot.Infra.Interfaces.AppServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AvaBot.Infra.AppServices;

public class WppConnectService : IWppConnectService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _secretKey;
    private readonly ILogger<WppConnectService> _logger;

    public WppConnectService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WppConnectService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _secretKey = configuration["WppConnect:SecretKey"] ?? "";
        _logger = logger;
    }

    public async Task<string> GenerateTokenAsync(string session)
    {
        var client = CreateClient();
        var response = await client.PostAsync($"/api/{session}/{_secretKey}/generate-token", null);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString() ?? "";
    }

    public async Task StartSessionAsync(string session, string webhookUrl)
    {
        var client = await CreateAuthenticatedClientAsync(session);

        var body = new { webhook = webhookUrl };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/{session}/start-session", content);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Sessao WPP Connect iniciada para {Session} com webhook {Url}", session, webhookUrl);
    }

    public async Task<string> GetQrCodeAsync(string session)
    {
        var client = await CreateAuthenticatedClientAsync(session);
        var response = await client.GetAsync($"/api/{session}/qrcode-session");
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (contentType.StartsWith("image/"))
        {
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var base64Img = Convert.ToBase64String(bytes);
            return $"data:{contentType};base64,{base64Img}";
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("qrcode", out var qrcode))
            return qrcode.GetString() ?? "";

        if (doc.RootElement.TryGetProperty("base64", out var base64))
            return base64.GetString() ?? "";

        return json;
    }

    public async Task<string> GetStatusAsync(string session)
    {
        var client = await CreateAuthenticatedClientAsync(session);
        var response = await client.GetAsync($"/api/{session}/status-session");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("status", out var status))
            return status.GetString() ?? "UNKNOWN";

        return "UNKNOWN";
    }

    public async Task CloseSessionAsync(string session)
    {
        var client = await CreateAuthenticatedClientAsync(session);
        var response = await client.PostAsync($"/api/{session}/close-session", null);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Sessao WPP Connect encerrada para {Session}", session);
    }

    public async Task SendMessageAsync(string session, string phone, string message)
    {
        var client = await CreateAuthenticatedClientAsync(session);

        var isGroup = phone.Contains("@g.us");
        var formattedPhone = phone.Contains("@") ? phone : $"{phone}@c.us";
        var body = new { phone = formattedPhone, isGroup, message };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var url = $"/api/{session}/send-message";
        _logger.LogDebug("Enviando mensagem WPP Connect: session={Session}, phone={Phone}, url={Url}", session, formattedPhone, url);

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao enviar mensagem WPP Connect: status={Status}, body={Body}", response.StatusCode, responseBody);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetBotLidAsync(string session)
    {
        var client = await CreateAuthenticatedClientAsync(session);

        string? phoneNumber = null;
        using (var hostJson = await TryGetJsonAsync(client, $"/api/{session}/host-device", session))
        {
            if (hostJson != null)
            {
                var lid = FindLidRecursive(hostJson.RootElement);
                if (!string.IsNullOrEmpty(lid)) return lid;

                phoneNumber = FindStringProperty(hostJson.RootElement, "phoneNumber")
                    ?? FindStringProperty(hostJson.RootElement, "wid");
            }
        }

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            foreach (var path in new[]
            {
                $"/api/{session}/contact/{phoneNumber}",
                $"/api/{session}/profile/{phoneNumber}",
                $"/api/{session}/check-number-status/{phoneNumber}"
            })
            {
                using var json = await TryGetJsonAsync(client, path, session);
                if (json == null) continue;
                var lid = FindLidRecursive(json.RootElement);
                if (!string.IsNullOrEmpty(lid))
                {
                    _logger.LogInformation("LID obtido via {Path}. session={Session} lid={Lid}", path, session, lid);
                    return lid;
                }
            }
        }

        _logger.LogWarning("LID nao encontrado em nenhum endpoint. session={Session} phoneNumber={Phone}", session, phoneNumber);
        return null;
    }

    private async Task<JsonDocument?> TryGetJsonAsync(System.Net.Http.HttpClient client, string path, string session)
    {
        try
        {
            var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("{Path} retornou {Status}. session={Session} body={Body}", path, response.StatusCode, session, body);
                return null;
            }
            _logger.LogDebug("{Path} response. session={Session} body={Body}", path, session, body);
            return JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Erro ao consultar {Path}. session={Session}", path, session);
            return null;
        }
    }

    private static string? FindStringProperty(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals(propertyName) && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    var nested = FindStringProperty(prop.Value, propertyName);
                    if (!string.IsNullOrEmpty(nested)) return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindStringProperty(item, propertyName);
                    if (!string.IsNullOrEmpty(nested)) return nested;
                }
                break;
        }
        return null;
    }

    private static string? FindLidRecursive(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals("lid") && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                    if (prop.Value.ValueKind == JsonValueKind.Object
                        && prop.Value.TryGetProperty("server", out var server)
                        && server.ValueKind == JsonValueKind.String
                        && server.GetString() == "lid"
                        && prop.Value.TryGetProperty("_serialized", out var ser)
                        && ser.ValueKind == JsonValueKind.String)
                    {
                        return ser.GetString();
                    }
                    var nested = FindLidRecursive(prop.Value);
                    if (!string.IsNullOrEmpty(nested)) return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindLidRecursive(item);
                    if (!string.IsNullOrEmpty(nested)) return nested;
                }
                break;
        }
        return null;
    }

    private HttpClient CreateClient()
    {
        return _httpClientFactory.CreateClient("WppConnect");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string session)
    {
        var client = CreateClient();
        var token = await GenerateTokenAsync(session);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
