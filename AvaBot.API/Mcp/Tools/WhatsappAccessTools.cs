using System.ComponentModel;
using FluentValidation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>
/// Controle de acesso do WhatsApp (feature 018, contrato mcp-tools.md): modo, frases prontas, contatos autorizados
/// e pedidos de acesso. Usa o mesmo WhatsappAccessService e os mesmos validators do painel. Nenhuma ferramenta
/// le ou grava o token do WhatsApp.
/// </summary>
[McpServerToolType]
public sealed class WhatsappAccessTools
{
    private static readonly Dictionary<string, WhatsappAccessMessage> MessageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["oferta"] = WhatsappAccessMessage.Offer,
        ["confirmacao"] = WhatsappAccessMessage.Confirmation,
        ["aguardando"] = WhatsappAccessMessage.Waiting,
        ["recusa"] = WhatsappAccessMessage.Declined
    };

    private static readonly Dictionary<string, int> StatusNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pendente"] = (int)WhatsappAccessRequestStatus.Pending,
        ["aprovada"] = (int)WhatsappAccessRequestStatus.Approved,
        ["rejeitada"] = (int)WhatsappAccessRequestStatus.Rejected
    };

    private readonly AgentService _agentService;
    private readonly WhatsappAccessService _accessService;
    private readonly IValidator<WhatsappAccessConfigUpdateInfo> _configValidator;
    private readonly IValidator<WhatsappContactInsertInfo> _contactValidator;
    private readonly IValidator<WhatsappAccessRequestApproveInfo> _approveValidator;
    private readonly ILogger<WhatsappAccessTools> _logger;

    public WhatsappAccessTools(
        AgentService agentService,
        WhatsappAccessService accessService,
        IValidator<WhatsappAccessConfigUpdateInfo> configValidator,
        IValidator<WhatsappContactInsertInfo> contactValidator,
        IValidator<WhatsappAccessRequestApproveInfo> approveValidator,
        ILogger<WhatsappAccessTools> logger)
    {
        _agentService = agentService;
        _accessService = accessService;
        _configValidator = configValidator;
        _contactValidator = contactValidator;
        _approveValidator = approveValidator;
        _logger = logger;
    }

    private async Task<Agent> ResolveAgentAsync(long agentId, McpCaller caller) =>
        await _agentService.GetOwnedByIdAsync(agentId, caller.UserId) ?? throw new KeyNotFoundException(AgentTools.NotFoundMessage);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T info)
    {
        var validation = await validator.ValidateAsync(info);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
    }

    // ------------------------------------------------------------------ Configuracao

    [McpServerTool(Name = "obter_acesso_whatsapp", Title = "Controle de acesso do WhatsApp", ReadOnly = true, Idempotent = true)]
    [Description("Modo de acesso do WhatsApp do agente (1 = todos, 2 = somente contatos autorizados), as quatro frases prontas do fluxo de solicitacao (texto efetivo, se e o padrao e o texto padrao) e as quantidades de contatos e de solicitacoes pendentes.")]
    public Task<CallToolResult> ObterAcessoWhatsapp(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ObterAcessoWhatsappAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ObterAcessoWhatsappAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "obter_acesso_whatsapp", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.GetConfigAsync(agent);
    });

    [McpServerTool(Name = "atualizar_acesso_whatsapp", Title = "Atualizar controle de acesso do WhatsApp", ReadOnly = false, Idempotent = true)]
    [Description("Define o modo de acesso do WhatsApp (1 = todos, 2 = somente contatos autorizados) e, opcionalmente, personaliza as frases prontas (ate 500 caracteres cada). Frases omitidas sao mantidas; para voltar ao texto padrao, liste a frase em restoreDefaults (oferta, confirmacao, aguardando, recusa). No modo 2, quem nao estiver na lista recebe so as frases prontas e nunca chega a IA.")]
    public Task<CallToolResult> AtualizarAcessoWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("1 = todos, 2 = somente contatos autorizados.")] int mode,
        [Description("Frase de oferta (pergunta se a pessoa quer solicitar acesso, com resposta sim/nao).")] string? offerMessage = null,
        [Description("Frase de confirmacao apos o 'sim'.")] string? confirmationMessage = null,
        [Description("Frase enviada enquanto o pedido aguarda aprovacao.")] string? waitingMessage = null,
        [Description("Frase enviada apos o 'nao'.")] string? declinedMessage = null,
        [Description("Frases que voltam ao texto padrao: oferta, confirmacao, aguardando, recusa.")] string[]? restoreDefaults = null)
        => AtualizarAcessoWhatsappAsync(McpCaller.From(context), agentId, mode, offerMessage, confirmationMessage, waitingMessage, declinedMessage, restoreDefaults);

    public Task<CallToolResult> AtualizarAcessoWhatsappAsync(McpCaller caller, long agentId, int mode, string? offerMessage, string? confirmationMessage,
        string? waitingMessage, string? declinedMessage, string[]? restoreDefaults) => McpToolResults.RunAsync(_logger, "atualizar_acesso_whatsapp", async () =>
    {
        var restore = new HashSet<WhatsappAccessMessage>();
        foreach (var name in restoreDefaults ?? [])
        {
            if (!MessageNames.TryGetValue(name.Trim(), out var kind))
                throw new InvalidOperationException($"Frase desconhecida em restoreDefaults: '{name}'. Use oferta, confirmacao, aguardando ou recusa.");
            restore.Add(kind);
        }

        var agent = await ResolveAgentAsync(agentId, caller);

        // Atualizacao parcial: frase omitida mantem o valor atual; restoreDefaults volta ao padrao (null).
        var info = new WhatsappAccessConfigUpdateInfo
        {
            Mode = mode,
            OfferMessage = Merge(WhatsappAccessMessage.Offer, offerMessage, agent.WhatsappOfferMessage),
            ConfirmationMessage = Merge(WhatsappAccessMessage.Confirmation, confirmationMessage, agent.WhatsappConfirmationMessage),
            WaitingMessage = Merge(WhatsappAccessMessage.Waiting, waitingMessage, agent.WhatsappWaitingMessage),
            DeclinedMessage = Merge(WhatsappAccessMessage.Declined, declinedMessage, agent.WhatsappDeclinedMessage)
        };
        await ValidateAsync(_configValidator, info);
        return await _accessService.UpdateConfigAsync(agent, info);

        string? Merge(WhatsappAccessMessage kind, string? informed, string? current) =>
            restore.Contains(kind) ? null : informed ?? current;
    });

    // ------------------------------------------------------------------ Contatos

    [McpServerTool(Name = "listar_contatos_whatsapp", Title = "Contatos autorizados do WhatsApp", ReadOnly = true, Idempotent = true)]
    [Description("Contatos autorizados a usar o agente pelo WhatsApp quando o modo e 2 (somente contatos autorizados): nome, telefone, identificador do WhatsApp e origem (1 = manual, 2 = aprovacao de pedido).")]
    public Task<CallToolResult> ListarContatosWhatsapp(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ListarContatosWhatsappAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ListarContatosWhatsappAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "listar_contatos_whatsapp", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.ListContactsAsync(agent);
    });

    [McpServerTool(Name = "adicionar_contato_whatsapp", Title = "Autorizar contato no WhatsApp", ReadOnly = false, Idempotent = false)]
    [Description("Autoriza um contato (nome e telefone) a usar o agente pelo WhatsApp. O telefone pode ter qualquer formatacao; sem codigo do pais assume Brasil (55). Telefone repetido no agente e recusado (o nono digito de celulares brasileiros e ignorado na comparacao). Se houver pedido pendente desse telefone, ele fica aprovado.")]
    public Task<CallToolResult> AdicionarContatoWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Nome do contato (ate 120 caracteres).")] string name,
        [Description("Telefone com DDD, com ou sem codigo do pais.")] string phone)
        => AdicionarContatoWhatsappAsync(McpCaller.From(context), agentId, name, phone);

    public Task<CallToolResult> AdicionarContatoWhatsappAsync(McpCaller caller, long agentId, string name, string phone) => McpToolResults.RunAsync(_logger, "adicionar_contato_whatsapp", async () =>
    {
        var info = new WhatsappContactInsertInfo { Name = name ?? string.Empty, Phone = phone ?? string.Empty };
        await ValidateAsync(_contactValidator, info);
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.AddContactAsync(agent, info);
    });

    [McpServerTool(Name = "atualizar_contato_whatsapp", Title = "Editar contato autorizado", ReadOnly = false, Idempotent = true)]
    [Description("Altera o nome e o telefone de um contato autorizado do WhatsApp. A mudanca vale a partir da proxima mensagem recebida.")]
    public Task<CallToolResult> AtualizarContatoWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do contato (whatsappContactId).")] long contactId,
        [Description("Nome do contato (ate 120 caracteres).")] string name,
        [Description("Telefone com DDD, com ou sem codigo do pais.")] string phone)
        => AtualizarContatoWhatsappAsync(McpCaller.From(context), agentId, contactId, name, phone);

    public Task<CallToolResult> AtualizarContatoWhatsappAsync(McpCaller caller, long agentId, long contactId, string name, string phone) => McpToolResults.RunAsync(_logger, "atualizar_contato_whatsapp", async () =>
    {
        var info = new WhatsappContactInsertInfo { Name = name ?? string.Empty, Phone = phone ?? string.Empty };
        await ValidateAsync(_contactValidator, info);
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.UpdateContactAsync(agent, contactId, info);
    });

    [McpServerTool(Name = "remover_contato_whatsapp", Title = "Remover contato autorizado", ReadOnly = false, Idempotent = true, Destructive = true)]
    [Description("Remove um contato autorizado do WhatsApp. No modo 2, a pessoa deixa de ser atendida pela IA e volta a receber a oferta de solicitacao de acesso.")]
    public Task<CallToolResult> RemoverContatoWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do contato (whatsappContactId).")] long contactId)
        => RemoverContatoWhatsappAsync(McpCaller.From(context), agentId, contactId);

    public Task<CallToolResult> RemoverContatoWhatsappAsync(McpCaller caller, long agentId, long contactId) => McpToolResults.RunAsync(_logger, "remover_contato_whatsapp", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        await _accessService.RemoveContactAsync(agent, contactId);
        return new McpMessageInfo { Message = "Contato removido" };
    });

    // ------------------------------------------------------------------ Pedidos de acesso

    [McpServerTool(Name = "listar_solicitacoes_whatsapp", Title = "Solicitacoes de acesso do WhatsApp", ReadOnly = true, Idempotent = true)]
    [Description("Pedidos de acesso feitos pelo WhatsApp por pessoas fora da lista (respondendo 'sim' a oferta), mais recentes primeiro: nome de perfil, telefone (se conhecido), identificador do WhatsApp e situacao (1 = pendente, 2 = aprovada, 3 = rejeitada). Filtro opcional por status: pendente, aprovada ou rejeitada.")]
    public Task<CallToolResult> ListarSolicitacoesWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Filtro: pendente, aprovada ou rejeitada. Omitido = todas.")] string? status = null)
        => ListarSolicitacoesWhatsappAsync(McpCaller.From(context), agentId, status);

    public Task<CallToolResult> ListarSolicitacoesWhatsappAsync(McpCaller caller, long agentId, string? status) => McpToolResults.RunAsync(_logger, "listar_solicitacoes_whatsapp", async () =>
    {
        int? statusValue = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!StatusNames.TryGetValue(status.Trim(), out var parsed))
                throw new InvalidOperationException($"Status invalido: '{status}'. Use pendente, aprovada ou rejeitada.");
            statusValue = parsed;
        }

        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.ListRequestsAsync(agent, statusValue);
    });

    [McpServerTool(Name = "aprovar_solicitacao_whatsapp", Title = "Aprovar solicitacao de acesso", ReadOnly = false, Idempotent = false)]
    [Description("Aprova uma solicitacao de acesso recebida pelo WhatsApp e cria o contato autorizado; a pessoa passa a ser atendida pela IA na proxima mensagem. O nome e opcional (padrao: nome de perfil do WhatsApp). Tambem aprova uma solicitacao que tinha sido rejeitada.")]
    public Task<CallToolResult> AprovarSolicitacaoWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id da solicitacao (whatsappAccessRequestId).")] long requestId,
        [Description("Nome do contato a criar (ate 120 caracteres).")] string? name = null)
        => AprovarSolicitacaoWhatsappAsync(McpCaller.From(context), agentId, requestId, name);

    public Task<CallToolResult> AprovarSolicitacaoWhatsappAsync(McpCaller caller, long agentId, long requestId, string? name) => McpToolResults.RunAsync(_logger, "aprovar_solicitacao_whatsapp", async () =>
    {
        await ValidateAsync(_approveValidator, new WhatsappAccessRequestApproveInfo { Name = name });
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.ApproveRequestAsync(agent, requestId, name, caller.UserId);
    });

    [McpServerTool(Name = "rejeitar_solicitacao_whatsapp", Title = "Rejeitar solicitacao de acesso", ReadOnly = false, Idempotent = false, Destructive = true)]
    [Description("Rejeita uma solicitacao de acesso pendente. A pessoa nao e avisada e as proximas mensagens dela sao ignoradas em silencio, ate o usuario aprovar a solicitacao ou cadastrar o contato.")]
    public Task<CallToolResult> RejeitarSolicitacaoWhatsapp(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id da solicitacao (whatsappAccessRequestId).")] long requestId)
        => RejeitarSolicitacaoWhatsappAsync(McpCaller.From(context), agentId, requestId);

    public Task<CallToolResult> RejeitarSolicitacaoWhatsappAsync(McpCaller caller, long agentId, long requestId) => McpToolResults.RunAsync(_logger, "rejeitar_solicitacao_whatsapp", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _accessService.RejectRequestAsync(agent, requestId, caller.UserId);
    });
}
