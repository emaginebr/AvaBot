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

        var body = new { webhook = new { url = webhookUrl } };
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

        var formattedPhone = phone.Contains("@c.us") ? phone : $"{phone}@c.us";
        var body = new { phone = formattedPhone, isGroup = false, message };
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
