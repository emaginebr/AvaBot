using System.Text.Json;
using AvaBot.Domain.Enums;
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
    private readonly WhatsappAccessService _accessService;
    private readonly ILogger<WhatsappService> _logger;
    private readonly string _webhookBaseUrl;
    private static readonly long _startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _botLidCache = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _startLocks = new();

    public WhatsappService(
        IWppConnectService wppConnect,
        IAgentRepository<Agent> agentRepo,
        AgentService agentService,
        ChatService chatService,
        WhatsappAccessService accessService,
        IConfiguration configuration,
        ILogger<WhatsappService> logger)
    {
        _wppConnect = wppConnect;
        _agentRepo = agentRepo;
        _agentService = agentService;
        _chatService = chatService;
        _accessService = accessService;
        _logger = logger;
        _webhookBaseUrl = configuration["WppConnect:WebhookBaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<WhatsappStatusInfo> StartSessionAsync(string slug, long ownerUserId)
    {
        var agent = await _agentService.GetOwnedBySlugAsync(slug, ownerUserId)
            ?? throw new KeyNotFoundException("Agente nao encontrado");

        var sessionName = slug;
        var webhookUrl = $"{_webhookBaseUrl}/whatsapp/{slug}/webhook";

        // Dois cliques (ou duas abas) no mesmo agente nao podem decidir ao mesmo tempo:
        // o segundo espera o primeiro e ja encontra a sessao em andamento.
        var gate = _startLocks.GetOrAdd(sessionName, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var current = await TryGetSessionStateAsync(sessionName);

            // Idempotente: com a sessao conectada ou ja pareando, nao se mexe nela. Fechar e
            // reabrir uma sessao em andamento derrubava o navegador do WPP Connect no meio do
            // caminho e o novo start falhava com "The browser is already running" (o WPP Connect
            // marca a sessao como fechada antes de o Chromium terminar de sair). O QR Code
            // nao precisa de sessao nova: o WPP Connect acompanha a troca do QR pelo WhatsApp
            // e o status devolve sempre o atual.
            if (IsConnectedStatus(current.Status) || IsInProgressStatus(current.Status))
            {
                _logger.LogInformation("Sessao WPP Connect ja em andamento, start ignorado. session={Session} status={Status}",
                    sessionName, current.Status);
                return ToStatusInfo(agent.Slug, current);
            }

            // ERROR: o WPP Connect nao conseguiu fechar o navegador de uma tentativa anterior e
            // so libera o perfil com um close-session explicito.
            if (current.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _wppConnect.CloseSessionAsync(sessionName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao limpar sessao em ERROR antes de iniciar. session={Session}", sessionName);
                }
            }

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
                Status = "INITIALIZING",
                IsConnected = false
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Estados do WPP Connect em que a sessao esta abrindo, pareando ou fechando.</summary>
    private static readonly string[] InProgressStatuses = { "INITIALIZING", "QRCODE", "PHONECODE", "CLOSING" };

    private static bool IsConnectedStatus(string status) =>
        status.Equals("CONNECTED", StringComparison.OrdinalIgnoreCase);

    private static bool IsInProgressStatus(string status) =>
        InProgressStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    private static WhatsappStatusInfo ToStatusInfo(string slug, WppSessionState state)
    {
        var connected = IsConnectedStatus(state.Status);
        return new WhatsappStatusInfo
        {
            AgentSlug = slug,
            Status = state.Status,
            IsConnected = connected,
            QrCode = connected ? null : state.QrCode
        };
    }

    private async Task<WppSessionState> TryGetSessionStateAsync(string session)
    {
        try
        {
            return await _wppConnect.GetSessionStateAsync(session);
        }
        catch (Exception ex)
        {
            // Sessao nunca criada (ou WPP Connect sem o client): trata como fechada.
            _logger.LogDebug(ex, "Sem estado de sessao no WPP Connect. session={Session}", session);
            return new WppSessionState("CLOSED", null);
        }
    }

    public async Task<WhatsappQrCodeInfo> GetQrCodeAsync(string slug, long ownerUserId)
    {
        var agent = await ResolveAgentAsync(slug, ownerUserId);
        var qrCode = await _wppConnect.GetQrCodeAsync(slug);

        return new WhatsappQrCodeInfo
        {
            AgentSlug = agent.Slug,
            QrCode = qrCode
        };
    }

    public async Task<WhatsappStatusInfo> GetStatusAsync(string slug, long ownerUserId)
    {
        var agent = await ResolveAgentAsync(slug, ownerUserId);
        var state = await _wppConnect.GetSessionStateAsync(slug);

        return ToStatusInfo(agent.Slug, state);
    }

    public async Task<WhatsappStatusInfo> DisconnectAsync(string slug, long ownerUserId)
    {
        var agent = await ResolveAgentAsync(slug, ownerUserId);

        // logout-session invalida de fato a sessao do WhatsApp (equivalente a "sair" pelo
        // celular). close-session sozinho so fecha o browser/puppeteer e mantem os dados
        // de autenticacao salvos, fazendo o WPP Connect reconectar sozinho em seguida.
        try
        {
            await _wppConnect.LogoutSessionAsync(slug);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao fazer logout da sessao WPP Connect, tentando close-session. agente={Slug}", slug);
            await _wppConnect.CloseSessionAsync(slug);
        }

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
            // WhatsApp usa LID (privacy id) em mentionedJidList. Buscamos o LID do bot via host-device e cacheamos.
            var botLid = await GetBotLidCachedAsync(slug, fromNumber);
            var isMentioned = false;
            if (!string.IsNullOrEmpty(botLid)
                && payload.TryGetProperty("mentionedJidList", out var mentionedList)
                && mentionedList.ValueKind == JsonValueKind.Array)
            {
                foreach (var mentioned in mentionedList.EnumerateArray())
                {
                    if (mentioned.ValueKind == JsonValueKind.String
                        && string.Equals(mentioned.GetString(), botLid, StringComparison.OrdinalIgnoreCase))
                    {
                        isMentioned = true;
                        break;
                    }
                }
            }

            if (!isMentioned)
            {
                _logger.LogInformation("[WhatsApp Webhook] Mensagem de grupo sem mencao ao agente, ignorada. agente={Slug} from={From}", slug, fromNumber);
                return;
            }

            _logger.LogInformation("[WhatsApp Webhook] Agente mencionado em grupo. agente={Slug} from={From}", slug, fromNumber);
        }

        // Agente carregado antes da checagem de tipo: o modo de acesso (feature 018) fica no agente.
        var resolvedAgent = await _agentService.GetBySlugAsync(slug);
        if (resolvedAgent == null || resolvedAgent.Status == 0 || string.IsNullOrEmpty(resolvedAgent.WhatsappToken))
        {
            _logger.LogWarning("[WhatsApp Webhook] Agente invalido ou inativo: {Slug}", slug);
            return;
        }

        // Controle de acesso (feature 018): antes de rejeicao de tipo, sessao e IA.
        if (resolvedAgent.WhatsappAccessMode == WhatsappAccessMode.Restricted)
        {
            var sender = WhatsappSender.FromPayload(payload);
            if (sender.Phone == null && sender.IsLid)
                sender.Phone = await _wppConnect.GetPhoneByLidAsync(slug, sender.WhatsappId);

            var decision = await _accessService.EvaluateIncomingAsync(resolvedAgent, sender, msgType == "chat" ? messageBody : null);
            if (decision.Kind == WhatsappAccessDecisionKind.Ignore)
                return;

            if (decision.Kind == WhatsappAccessDecisionKind.Reply)
            {
                // Reply so acontece em conversa privada: responde no mesmo destino que a IA usaria.
                var replyTarget = fromNumber.Replace("@c.us", "");
                try
                {
                    if (!string.IsNullOrEmpty(replyTarget) && !string.IsNullOrEmpty(decision.Text))
                        await _wppConnect.SendMessageAsync(slug, replyTarget, decision.Text);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[WhatsApp Acesso] Falha ao enviar frase pronta. agente={Slug} destino={Target}", slug, replyTarget);
                }
                return;
            }
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

    private async Task<string?> GetBotLidCachedAsync(string slug, string? groupId = null)
    {
        if (_botLidCache.TryGetValue(slug, out var cached))
            return cached;

        try
        {
            var lid = await _wppConnect.GetBotLidAsync(slug, groupId);
            if (!string.IsNullOrEmpty(lid))
            {
                _botLidCache[slug] = lid;
                _logger.LogInformation("[WhatsApp] LID do bot cacheado. agente={Slug} lid={Lid}", slug, lid);
            }
            return lid;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[WhatsApp] Falha ao obter LID do bot. agente={Slug}", slug);
            return null;
        }
    }

    private async Task<Agent> ResolveAgentAsync(string slug, long ownerUserId)
    {
        var agent = await _agentService.GetOwnedBySlugAsync(slug, ownerUserId)
            ?? throw new KeyNotFoundException("Agente nao encontrado");

        if (string.IsNullOrEmpty(agent.WhatsappToken))
            throw new InvalidOperationException("Agente nao possui sessao WhatsApp ativa. Chame /start-session primeiro.");

        return agent;
    }
}
