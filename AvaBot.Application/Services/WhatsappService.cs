using System.Text.Json;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AvaBot.Application.Services;

public class WhatsappService
{
    private readonly IWppConnectService _wppConnect;
    private readonly IAgentRepository<Agent> _agentRepo;
    private readonly AgentService _agentService;
    private readonly ChatService _chatService;
    private readonly ILogger<WhatsappService> _logger;
    private readonly string _webhookBaseUrl;

    public WhatsappService(
        IWppConnectService wppConnect,
        IAgentRepository<Agent> agentRepo,
        AgentService agentService,
        ChatService chatService,
        IConfiguration configuration,
        ILogger<WhatsappService> logger)
    {
        _wppConnect = wppConnect;
        _agentRepo = agentRepo;
        _agentService = agentService;
        _chatService = chatService;
        _logger = logger;
        _webhookBaseUrl = configuration["WppConnect:WebhookBaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<WhatsappStatusInfo> StartSessionAsync(string slug)
    {
        var agent = await _agentService.GetBySlugAsync(slug)
            ?? throw new KeyNotFoundException($"Agente '{slug}' nao encontrado");

        var sessionName = slug;
        var webhookUrl = $"{_webhookBaseUrl}/whatsapp/{slug}/webhook";

        // Gerar token no WPP Connect e salvar no agente
        var token = await _wppConnect.GenerateTokenAsync(sessionName);

        agent.WhatsappToken = token;
        await _agentRepo.UpdateAsync(agent);

        // Iniciar sessao com webhook
        await _wppConnect.StartSessionAsync(sessionName, webhookUrl);

        _logger.LogInformation("Sessao WhatsApp iniciada para agente {Slug}", slug);

        return new WhatsappStatusInfo
        {
            AgentSlug = agent.Slug,
            Status = "STARTING",
            IsConnected = false
        };
    }

    public async Task<WhatsappQrCodeInfo> GetQrCodeAsync(string slug)
    {
        var agent = await ResolveAgentAsync(slug);
        var qrCode = await _wppConnect.GetQrCodeAsync(slug);

        return new WhatsappQrCodeInfo
        {
            AgentSlug = agent.Slug,
            QrCode = qrCode
        };
    }

    public async Task<WhatsappStatusInfo> GetStatusAsync(string slug)
    {
        var agent = await ResolveAgentAsync(slug);
        var status = await _wppConnect.GetStatusAsync(slug);

        return new WhatsappStatusInfo
        {
            AgentSlug = agent.Slug,
            Status = status,
            IsConnected = status.Equals("CONNECTED", StringComparison.OrdinalIgnoreCase)
        };
    }

    public async Task<WhatsappStatusInfo> DisconnectAsync(string slug)
    {
        var agent = await ResolveAgentAsync(slug);
        await _wppConnect.CloseSessionAsync(slug);

        return new WhatsappStatusInfo
        {
            AgentSlug = agent.Slug,
            Status = "DISCONNECTED",
            IsConnected = false
        };
    }

    public async Task ProcessWebhookAsync(string slug, JsonElement payload)
    {
        if (!payload.TryGetProperty("event", out var eventProp) || eventProp.GetString() != "onmessage")
            return;

        if (!payload.TryGetProperty("data", out var data))
            return;

        if (data.TryGetProperty("isGroupMsg", out var isGroup) && isGroup.GetBoolean())
            return;

        if (!data.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "chat")
        {
            if (data.TryGetProperty("from", out var fromReject))
            {
                var rejectPhone = fromReject.GetString()?.Replace("@c.us", "") ?? "";
                try { await _wppConnect.SendMessageAsync(slug, rejectPhone, "Desculpe, eu so consigo processar mensagens de texto."); }
                catch { /* ignore send errors for rejection message */ }
            }
            return;
        }

        var fromNumber = data.TryGetProperty("from", out var fromProp) ? fromProp.GetString() ?? "" : "";
        var messageBody = data.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

        if (string.IsNullOrEmpty(fromNumber) || string.IsNullOrEmpty(messageBody))
            return;

        var phone = fromNumber.Replace("@c.us", "");

        var resolvedAgent = await _agentService.GetBySlugAsync(slug);
        if (resolvedAgent == null || resolvedAgent.Status == 0 || string.IsNullOrEmpty(resolvedAgent.WhatsappToken))
        {
            _logger.LogWarning("Webhook recebido para agente invalido ou inativo: {Slug}", slug);
            return;
        }

        try
        {
            var session = await _chatService.CreateSessionAsync(
                resolvedAgent.AgentId,
                phone,
                null,
                null);

            var fullResponse = string.Empty;
            await foreach (var token in _chatService.ProcessMessageAsync(
                resolvedAgent.AgentId,
                session.ChatSessionId,
                resolvedAgent.ChatModel,
                resolvedAgent.SystemPrompt,
                messageBody))
            {
                fullResponse += token;
            }

            if (!string.IsNullOrEmpty(fullResponse))
                await _wppConnect.SendMessageAsync(slug, phone, fullResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem WhatsApp do {Phone} para agente {Slug}", phone, slug);
        }
    }

    private async Task<Agent> ResolveAgentAsync(string slug)
    {
        var agent = await _agentService.GetBySlugAsync(slug)
            ?? throw new KeyNotFoundException($"Agente '{slug}' nao encontrado");

        if (string.IsNullOrEmpty(agent.WhatsappToken))
            throw new InvalidOperationException("Agente nao possui sessao WhatsApp ativa. Chame /start-session primeiro.");

        return agent;
    }
}
