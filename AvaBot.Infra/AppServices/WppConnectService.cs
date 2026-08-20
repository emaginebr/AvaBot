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
        // Logo apos start-session, o WPP Connect ainda esta subindo o navegador/WhatsApp Web
        // e pode nao ter gerado o QR ainda (client.urlcode fica vazio ate o primeiro catchQR).
        // Sem retry, uma chamada prematura retorna o JSON de "QRCode is not available" em vez
        // de uma imagem, fazendo o admin exibir um QR desatualizado/errado.
        const int maxAttempts = 15;
        var delay = TimeSpan.FromSeconds(1);

        _logger.LogDebug("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] GetQrCodeAsync iniciado. session={Session}",
            DateTimeOffset.Now, session);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var client = await CreateAuthenticatedClientAsync(session);
            var response = await client.GetAsync($"/api/{session}/qrcode-session");
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            _logger.LogDebug("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] GetQrCodeAsync tentativa {Attempt}/{MaxAttempts}. session={Session} contentType={ContentType}",
                DateTimeOffset.Now, attempt, maxAttempts, session, contentType);

            if (contentType.StartsWith("image/"))
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64Img = Convert.ToBase64String(bytes);

                _logger.LogDebug("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] QR code obtido. session={Session} tentativa={Attempt} tamanhoBytes={Bytes} hashBase64={Hash}",
                    DateTimeOffset.Now, session, attempt, bytes.Length, base64Img.GetHashCode());

                return $"data:{contentType};base64,{base64Img}";
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] QR code ainda nao disponivel. session={Session} tentativa={Attempt} resposta={Json}",
                DateTimeOffset.Now, session, attempt, json);

            if (attempt == maxAttempts)
            {
                _logger.LogWarning("[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] QR code nao ficou pronto a tempo. session={Session} ultimaResposta={Json}",
                    DateTimeOffset.Now, session, json);
                throw new InvalidOperationException("QR code ainda nao esta disponivel, tente novamente em instantes.");
            }

            await Task.Delay(delay);
        }

        throw new InvalidOperationException("QR code ainda nao esta disponivel, tente novamente em instantes.");
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

    public async Task<string?> GetBotLidAsync(string session, string? groupId = null)
    {
        if (string.IsNullOrEmpty(groupId))
            return null;

        var client = await CreateAuthenticatedClientAsync(session);
        var response = await client.GetAsync($"/api/{session}/group-members/{groupId}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Falha ao obter group-members. session={Session} groupId={GroupId} status={Status}",
                session, groupId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("response", out var members) || members.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var member in members.EnumerateArray())
        {
            if (!member.TryGetProperty("isMe", out var isMe) || isMe.ValueKind != JsonValueKind.True)
                continue;

            if (member.TryGetProperty("id", out var idProp)
                && idProp.ValueKind == JsonValueKind.Object
                && idProp.TryGetProperty("_serialized", out var ser)
                && ser.ValueKind == JsonValueKind.String)
            {
                return ser.GetString();
            }
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
