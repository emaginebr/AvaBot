using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

/// <summary>
/// Controle de acesso do WhatsApp (feature 018): configuracao, contatos autorizados, pedidos de acesso e a
/// decisao sobre cada mensagem recebida. Painel, MCP e webhook passam todos por aqui.
/// Os metodos recebem o agente ja resolvido e validado como do usuario pelo chamador.
/// </summary>
public class WhatsappAccessService
{
    public const string ContactNotFoundMessage = "Contato nao encontrado";
    public const string RequestNotFoundMessage = "Solicitacao nao encontrada";
    public const string DuplicatePhoneMessage = "Este telefone ja esta cadastrado neste agente";
    public const string InvalidPhoneMessage = "Telefone invalido. Informe DDD e numero, com ou sem codigo do pais";
    public const string RequestAlreadyApprovedMessage = "Solicitacao ja aprovada";
    public const string RequestNotPendingMessage = "Solicitacao nao esta pendente";
    public const string InvalidModeMessage = "Modo de acesso invalido";

    public static readonly TimeSpan AutoReplyInterval = TimeSpan.FromHours(1);
    public static readonly TimeSpan OfferAnswerWindow = TimeSpan.FromHours(24);

    private readonly IWhatsappContactRepository<WhatsappContact> _contacts;
    private readonly IWhatsappAccessRequestRepository<WhatsappAccessRequest> _requests;
    private readonly IWhatsappSenderStateRepository<WhatsappSenderState> _states;
    private readonly IAgentRepository<Agent> _agents;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly ILogger<WhatsappAccessService> _logger;

    public WhatsappAccessService(
        IWhatsappContactRepository<WhatsappContact> contacts,
        IWhatsappAccessRequestRepository<WhatsappAccessRequest> requests,
        IWhatsappSenderStateRepository<WhatsappSenderState> states,
        IAgentRepository<Agent> agents,
        IMapper mapper,
        TimeProvider time,
        ILogger<WhatsappAccessService> logger)
    {
        _contacts = contacts;
        _requests = requests;
        _states = states;
        _agents = agents;
        _mapper = mapper;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    // ------------------------------------------------------------------ Configuracao

    public async Task<WhatsappAccessConfigInfo> GetConfigAsync(Agent agent)
    {
        return new WhatsappAccessConfigInfo
        {
            Mode = (int)agent.WhatsappAccessMode,
            OfferMessage = BuildMessageInfo(agent.WhatsappOfferMessage, WhatsappAccessMessage.Offer),
            ConfirmationMessage = BuildMessageInfo(agent.WhatsappConfirmationMessage, WhatsappAccessMessage.Confirmation),
            WaitingMessage = BuildMessageInfo(agent.WhatsappWaitingMessage, WhatsappAccessMessage.Waiting),
            DeclinedMessage = BuildMessageInfo(agent.WhatsappDeclinedMessage, WhatsappAccessMessage.Declined),
            ContactCount = await _contacts.CountByAgentAsync(agent.AgentId),
            PendingRequestCount = await _requests.CountPendingAsync(agent.AgentId)
        };
    }

    public async Task<WhatsappAccessConfigInfo> UpdateConfigAsync(Agent agent, WhatsappAccessConfigUpdateInfo info)
    {
        if (!Enum.IsDefined(typeof(WhatsappAccessMode), info.Mode))
            throw new InvalidOperationException(InvalidModeMessage);

        agent.WhatsappAccessMode = (WhatsappAccessMode)info.Mode;
        agent.WhatsappOfferMessage = CleanMessage(info.OfferMessage);
        agent.WhatsappConfirmationMessage = CleanMessage(info.ConfirmationMessage);
        agent.WhatsappWaitingMessage = CleanMessage(info.WaitingMessage);
        agent.WhatsappDeclinedMessage = CleanMessage(info.DeclinedMessage);
        await _agents.UpdateAsync(agent);

        return await GetConfigAsync(agent);
    }

    private static WhatsappAccessMessageInfo BuildMessageInfo(string? custom, WhatsappAccessMessage kind)
    {
        var defaultText = Agent.GetDefaultWhatsappMessage(kind);
        return new WhatsappAccessMessageInfo
        {
            Text = custom ?? defaultText,
            IsDefault = custom == null,
            DefaultText = defaultText
        };
    }

    /// <summary>null restaura o padrao; texto vazio e barrado pelo validator, mas aqui tambem vira padrao.</summary>
    private static string? CleanMessage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var trimmed = text.Trim();
        return trimmed.Length > WhatsappAccessDefaults.MaxMessageLength
            ? trimmed[..WhatsappAccessDefaults.MaxMessageLength]
            : trimmed;
    }

    // ------------------------------------------------------------------ Contatos

    public async Task<List<WhatsappContactInfo>> ListContactsAsync(Agent agent)
    {
        var contacts = await _contacts.GetAllByAgentAsync(agent.AgentId);
        return _mapper.Map<List<WhatsappContactInfo>>(contacts);
    }

    public async Task<WhatsappContactInfo> AddContactAsync(Agent agent, WhatsappContactInsertInfo info)
    {
        var normalized = WhatsappPhone.Normalize(info.Phone) ?? throw new InvalidOperationException(InvalidPhoneMessage);
        var matchKey = WhatsappPhone.MatchKey(normalized);

        if (await _contacts.ExistsAsync(agent.AgentId, matchKey, null))
            throw new InvalidOperationException(DuplicatePhoneMessage);

        var contact = new WhatsappContact
        {
            AgentId = agent.AgentId,
            Name = info.Name.Trim(),
            Source = WhatsappContactSource.Manual
        };
        contact.SetPhone(info.Phone, normalized);
        contact = await _contacts.CreateAsync(contact);

        await ApprovePendingForPhoneAsync(agent, contact);
        return _mapper.Map<WhatsappContactInfo>(contact);
    }

    public async Task<WhatsappContactInfo> UpdateContactAsync(Agent agent, long contactId, WhatsappContactInsertInfo info)
    {
        var contact = await _contacts.GetByIdAsync(contactId, agent.AgentId)
            ?? throw new KeyNotFoundException(ContactNotFoundMessage);

        var normalized = WhatsappPhone.Normalize(info.Phone) ?? throw new InvalidOperationException(InvalidPhoneMessage);
        var matchKey = WhatsappPhone.MatchKey(normalized);

        if (await _contacts.ExistsAsync(agent.AgentId, matchKey, null, contact.WhatsappContactId))
            throw new InvalidOperationException(DuplicatePhoneMessage);

        // Um JID "@c.us" de outro numero deixa de valer; um "@lid" e mantido (nao revela o telefone).
        var jidPhone = WhatsappPhone.FromJid(contact.WhatsappId);
        if (jidPhone != null && WhatsappPhone.MatchKey(jidPhone) != matchKey)
            contact.WhatsappId = null;

        contact.Name = info.Name.Trim();
        contact.SetPhone(info.Phone, normalized);
        contact = await _contacts.UpdateAsync(contact);

        await ApprovePendingForPhoneAsync(agent, contact);
        return _mapper.Map<WhatsappContactInfo>(contact);
    }

    public async Task RemoveContactAsync(Agent agent, long contactId)
    {
        var contact = await _contacts.GetByIdAsync(contactId, agent.AgentId)
            ?? throw new KeyNotFoundException(ContactNotFoundMessage);
        await _contacts.DeleteAsync(contact);
    }

    /// <summary>FR-026: cadastrar manualmente quem tem pedido pendente encerra o pedido como aprovado.</summary>
    private async Task ApprovePendingForPhoneAsync(Agent agent, WhatsappContact contact)
    {
        if (string.IsNullOrEmpty(contact.PhoneMatchKey))
            return;

        var pending = await _requests.GetPendingByPhoneAsync(agent.AgentId, contact.PhoneMatchKey);
        if (pending == null)
            return;

        pending.Status = WhatsappAccessRequestStatus.Approved;
        pending.DecidedAt = Now;
        pending.WhatsappContactId = contact.WhatsappContactId;
        await _requests.UpdateAsync(pending);
    }

    // ------------------------------------------------------------------ Pedidos de acesso

    public async Task<List<WhatsappAccessRequestInfo>> ListRequestsAsync(Agent agent, int? status)
    {
        if (status.HasValue && !Enum.IsDefined(typeof(WhatsappAccessRequestStatus), status.Value))
            throw new InvalidOperationException("Status de solicitacao invalido");

        var requests = await _requests.GetByAgentAsync(agent.AgentId, status);
        return _mapper.Map<List<WhatsappAccessRequestInfo>>(requests);
    }

    public async Task<WhatsappContactInfo> ApproveRequestAsync(Agent agent, long requestId, string? name, long decidedByUserId)
    {
        var request = await _requests.GetByIdAsync(requestId, agent.AgentId)
            ?? throw new KeyNotFoundException(RequestNotFoundMessage);

        if (request.Status == WhatsappAccessRequestStatus.Approved)
            throw new InvalidOperationException(RequestAlreadyApprovedMessage);

        var contact = await _contacts.FindBySenderAsync(agent.AgentId, request.PhoneMatchKey, request.WhatsappId);
        if (contact == null)
        {
            contact = new WhatsappContact
            {
                AgentId = agent.AgentId,
                Name = FirstNotEmpty(name, request.ProfileName, request.PhoneNormalized, request.WhatsappId)!,
                WhatsappId = request.WhatsappId,
                Source = WhatsappContactSource.Approval
            };
            contact.SetPhone(request.PhoneNormalized, request.PhoneNormalized);
            contact = await _contacts.CreateAsync(contact);
        }
        else
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(name))
            {
                contact.Name = name.Trim();
                changed = true;
            }
            if (string.IsNullOrEmpty(contact.WhatsappId))
            {
                contact.WhatsappId = request.WhatsappId;
                changed = true;
            }
            if (changed)
                contact = await _contacts.UpdateAsync(contact);
        }

        request.Status = WhatsappAccessRequestStatus.Approved;
        request.DecidedAt = Now;
        request.DecidedByUserId = decidedByUserId;
        request.WhatsappContactId = contact.WhatsappContactId;
        await _requests.UpdateAsync(request);

        return _mapper.Map<WhatsappContactInfo>(contact);
    }

    public async Task<WhatsappAccessRequestInfo> RejectRequestAsync(Agent agent, long requestId, long decidedByUserId)
    {
        var request = await _requests.GetByIdAsync(requestId, agent.AgentId)
            ?? throw new KeyNotFoundException(RequestNotFoundMessage);

        if (request.Status != WhatsappAccessRequestStatus.Pending)
            throw new InvalidOperationException(RequestNotPendingMessage);

        request.Status = WhatsappAccessRequestStatus.Rejected;
        request.DecidedAt = Now;
        request.DecidedByUserId = decidedByUserId;
        await _requests.UpdateAsync(request);

        return _mapper.Map<WhatsappAccessRequestInfo>(request);
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));

    // ------------------------------------------------------------------ Webhook

    /// <summary>
    /// Decide o que fazer com uma mensagem recebida (contrato whatsapp-webhook-flow.md). Nunca aciona a IA:
    /// so devolve Allow (segue para o ChatService), Reply (frase pronta) ou Ignore.
    /// </summary>
    /// <param name="text">Texto da mensagem; null para tipos nao suportados (audio, imagem...).</param>
    public async Task<WhatsappAccessDecision> EvaluateIncomingAsync(Agent agent, WhatsappSender sender, string? text)
    {
        if (agent.WhatsappAccessMode != WhatsappAccessMode.Restricted)
            return WhatsappAccessDecision.Allow();

        var decision = await DecideAsync(agent, sender, text);
        if (decision.Kind != WhatsappAccessDecisionKind.Allow)
        {
            _logger.LogInformation("[WhatsApp Acesso] agente={Slug} remetente={WhatsappId} telefone={Phone} etapa={Stage}",
                agent.Slug, sender.WhatsappId, sender.Phone,
                decision.Kind == WhatsappAccessDecisionKind.Ignore ? $"Ignore:{decision.Reason}" : decision.Reason);
        }
        return decision;
    }

    private async Task<WhatsappAccessDecision> DecideAsync(Agent agent, WhatsappSender sender, string? text)
    {
        if (string.IsNullOrEmpty(sender.WhatsappId))
            return WhatsappAccessDecision.Ignore("remetente-desconhecido");

        var now = Now;
        var matchKey = WhatsappPhone.MatchKey(sender.Phone);

        // B. Contato autorizado (por telefone ou pelo JID)
        var contact = await _contacts.FindBySenderAsync(agent.AgentId, matchKey, sender.WhatsappId);
        if (contact != null)
        {
            await BackfillWhatsappIdAsync(contact, sender);
            return WhatsappAccessDecision.Allow();
        }

        // C. Grupo: nunca roda o fluxo de solicitacao
        if (sender.IsGroup)
            return WhatsappAccessDecision.Ignore("grupo-nao-autorizado");

        var state = await _states.GetAsync(agent.AgentId, sender.WhatsappId)
            ?? new WhatsappSenderState { AgentId = agent.AgentId, WhatsappId = sender.WhatsappId };

        // D. Pedido vigente
        var request = await _requests.GetCurrentForSenderAsync(agent.AgentId, sender.WhatsappId, matchKey);
        if (request?.Status == WhatsappAccessRequestStatus.Rejected)
            return WhatsappAccessDecision.Ignore("rejeitado");
        if (request?.Status == WhatsappAccessRequestStatus.Pending)
            return await LimitedReplyAsync(agent, state, WhatsappAccessMessage.Waiting, now);

        // E. Resposta a oferta em aberto
        var offerOpen = state.OfferSentAt.HasValue && now - state.OfferSentAt.Value <= OfferAnswerWindow;
        var reply = offerOpen ? WhatsappReplyParser.Parse(text) : WhatsappReply.None;

        if (reply == WhatsappReply.Yes)
        {
            try
            {
                await _requests.CreateAsync(new WhatsappAccessRequest
                {
                    AgentId = agent.AgentId,
                    WhatsappId = sender.WhatsappId,
                    PhoneNormalized = sender.Phone,
                    PhoneMatchKey = matchKey,
                    ProfileName = sender.ProfileName,
                    Status = WhatsappAccessRequestStatus.Pending,
                    RequestedAt = now
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogInformation(ex, "[WhatsApp Acesso] Pedido pendente ja existe. agente={Slug} remetente={WhatsappId}",
                    agent.Slug, sender.WhatsappId);
                return WhatsappAccessDecision.Ignore("pedido-duplicado");
            }

            state.OfferSentAt = null;
            state.LastAutoReplyAt = now;
            await _states.UpsertAsync(state);
            return WhatsappAccessDecision.Reply(agent.GetWhatsappMessage(WhatsappAccessMessage.Confirmation), nameof(WhatsappAccessMessage.Confirmation));
        }

        if (reply == WhatsappReply.No)
        {
            state.OfferSentAt = null;
            state.LastAutoReplyAt = now;
            await _states.UpsertAsync(state);
            return WhatsappAccessDecision.Reply(agent.GetWhatsappMessage(WhatsappAccessMessage.Declined), nameof(WhatsappAccessMessage.Declined));
        }

        return await LimitedReplyAsync(agent, state, WhatsappAccessMessage.Offer, now);
    }

    /// <summary>Oferta e aviso de espera: no maximo uma frase automatica por hora por remetente.</summary>
    private async Task<WhatsappAccessDecision> LimitedReplyAsync(Agent agent, WhatsappSenderState state, WhatsappAccessMessage kind, DateTime now)
    {
        if (state.LastAutoReplyAt.HasValue && now - state.LastAutoReplyAt.Value < AutoReplyInterval)
            return WhatsappAccessDecision.Ignore("limite-1h");

        state.LastAutoReplyAt = now;
        if (kind == WhatsappAccessMessage.Offer)
            state.OfferSentAt = now;
        await _states.UpsertAsync(state);

        return WhatsappAccessDecision.Reply(agent.GetWhatsappMessage(kind), kind.ToString());
    }

    /// <summary>
    /// Guarda o JID do contato reconhecido pelo telefone, para reconhece-lo depois mesmo que o WhatsApp
    /// mande so o LID e a resolucao do telefone falhe.
    /// </summary>
    private async Task BackfillWhatsappIdAsync(WhatsappContact contact, WhatsappSender sender)
    {
        if (string.Equals(contact.WhatsappId, sender.WhatsappId, StringComparison.OrdinalIgnoreCase))
            return;
        if (!string.IsNullOrEmpty(contact.WhatsappId) && !sender.IsLid)
            return;

        try
        {
            contact.WhatsappId = sender.WhatsappId;
            await _contacts.UpdateAsync(contact);
        }
        catch (Exception ex)
        {
            // O backfill e uma otimizacao: falhar aqui nao pode impedir o atendimento.
            _logger.LogWarning(ex, "[WhatsApp Acesso] Falha ao gravar o identificador do contato {ContactId}", contact.WhatsappContactId);
        }
    }
}
