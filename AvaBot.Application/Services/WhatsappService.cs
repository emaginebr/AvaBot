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
    private static readonly long _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
        _logger.LogInformation("Iniciando sessao WPP Connect: session={Session}, webhookUrl={WebhookUrl}", sessionName, webhookUrl);
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
        var eventName = payload.TryGetProperty("event", out var eventProp) ? eventProp.GetString() : null;
        _logger.LogInformation("[WhatsApp Webhook] Recebido evento={Event} agente={Slug} payload={Payload}",
            eventName, slug, payload.GetRawText());

        if (eventName != "onmessage")
            return;

        // Ignorar mensagens anteriores ao inicio da aplicacao
        if (payload.TryGetProperty("timestamp", out var tsProp) && tsProp.TryGetInt64(out var msgTimestamp))
        {
            if (msgTimestamp < _startTimestamp)
            {
                _logger.LogInformation("[WhatsApp Webhook] Mensagem antiga ignorada. agente={Slug} timestamp={Timestamp} appStart={AppStart}",
                    slug, msgTimestamp, _startTimestamp);
                return;
            }
        }

        // WPP Connect faz Object.assign({ event, session }, messageData) — campos no root, sem wrapper "data"
        var isGroupMsg = payload.TryGetProperty("isGroupMsg", out var isGroup) && isGroup.GetBoolean();
        var msgType = payload.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
        var fromNumber = payload.TryGetProperty("from", out var fromProp) ? fromProp.GetString() ?? "" : "";
        var messageBody = payload.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";

        _logger.LogInformation("[WhatsApp Webhook] agente={Slug} from={From} type={Type} isGroup={IsGroup} body={Body}",
            slug, fromNumber, msgType, isGroupMsg, messageBody);

        if (isGroupMsg)
        {
            // WhatsApp usa LID (privacy id) em mentionedJidList, mas "to" vem como @c.us.
            // Heuristica: se o body contem @<digitos> e mentionedJidList nao esta vazia, assumimos mencao ao bot.
            var isMentioned = false;
            if (payload.TryGetProperty("mentionedJidList", out var mentionedList)
                && mentionedList.ValueKind == JsonValueKind.Array
                && mentionedList.GetArrayLength() > 0
                && System.Text.RegularExpressions.Regex.IsMatch(messageBody, "@\\d+"))
            {
                isMentioned = true;
            }

            if (!isMentioned)
            {
                _logger.LogInformation("[WhatsApp Webhook] Mensagem de grupo sem mencao ao agente, ignorada. agente={Slug} from={From}", slug, fromNumber);
                return;
            }

            _logger.LogInformation("[WhatsApp Webhook] Agente mencionado em grupo. agente={Slug} from={From}", slug, fromNumber);
        }

        if (msgType != "chat")
        {
            _logger.LogInformation("[WhatsApp Webhook] Tipo nao suportado={Type}, enviando rejeicao. agente={Slug} from={From}", msgType, slug, fromNumber);
            if (!string.IsNullOrEmpty(fromNumber))
            {
                var rejectPhone = isGroupMsg ? fromNumber : fromNumber.Replace("@c.us", "");
                try { await _wppConnect.SendMessageAsync(slug, rejectPhone, "Desculpe, eu so consigo processar mensagens de texto."); }
                catch { /* ignore send errors for rejection message */ }
            }
            return;
        }

        if (string.IsNullOrEmpty(fromNumber) || string.IsNullOrEmpty(messageBody))
        {
            _logger.LogInformation("[WhatsApp Webhook] Mensagem vazia ou sem remetente, ignorando. agente={Slug}", slug);
            return;
        }

        var phone = isGroupMsg ? fromNumber : fromNumber.Replace("@c.us", "");

        var resolvedAgent = await _agentService.GetBySlugAsync(slug);
        if (resolvedAgent == null || resolvedAgent.Status == 0 || string.IsNullOrEmpty(resolvedAgent.WhatsappToken))
        {
            _logger.LogWarning("[WhatsApp Webhook] Agente invalido ou inativo: {Slug}", slug);
            return;
        }

        try
        {
            var sessionStatus = await _wppConnect.GetStatusAsync(slug);
            if (!sessionStatus.Equals("CONNECTED", StringComparison.OrdinalIgnoreCase)
                && !sessionStatus.Equals("inChat", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[WhatsApp Webhook] Sessao WPP nao conectada, mensagem descartada. agente={Slug} phone={Phone} status={Status}",
                    slug, phone, sessionStatus);
                return;
            }

            _logger.LogInformation("[WhatsApp Webhook] Processando mensagem. agente={Slug} phone={Phone} mensagem={Message}",
                slug, phone, messageBody);

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

            _logger.LogInformation("[WhatsApp Webhook] Resposta gerada. agente={Slug} phone={Phone} tamanho={Length} resposta={Response}",
                slug, phone, fullResponse.Length, fullResponse);

            if (!string.IsNullOrEmpty(fullResponse))
                await _wppConnect.SendMessageAsync(slug, phone, fullResponse);

            _logger.LogInformation("[WhatsApp Webhook] Resposta enviada com sucesso. agente={Slug} phone={Phone}", slug, phone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhatsApp Webhook] Erro ao processar mensagem. agente={Slug} phone={Phone}", slug, phone);
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
