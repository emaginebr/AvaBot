using System.ComponentModel;
using AutoMapper;
using FluentValidation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.Application.Services;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>
/// Ferramentas de agentes (contrato mcp-tools.md #2–#6). Nao existe ferramenta de exclusao (FR-008) e
/// nenhuma aceita chave de API ou token (FR-010): o guarda de segredos recusa a chamada antes daqui.
/// </summary>
[McpServerToolType]
public sealed class AgentTools
{
    public const string NotFoundMessage = "Agente nao encontrado";
    private const string DefaultChatModel = "gpt-4o";

    private readonly AgentService _agentService;
    private readonly IValidator<AgentInsertInfo> _insertValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<AgentTools> _logger;

    public AgentTools(AgentService agentService, IValidator<AgentInsertInfo> insertValidator, IMapper mapper, ILogger<AgentTools> logger)
    {
        _agentService = agentService;
        _insertValidator = insertValidator;
        _mapper = mapper;
        _logger = logger;
    }

    [McpServerTool(Name = "listar_agentes", Title = "Listar agentes", ReadOnly = true, Idempotent = true)]
    [Description("Lista os agentes da conta com id, nome, endereco (slug), modelo, se esta ativo, se ha chave de API configurada (sem o valor), se o Power BI esta ligado e o nome do bot do Telegram. Use obter_agente para ver o prompt e os detalhes.")]
    public Task<CallToolResult> ListarAgentes(RequestContext<CallToolRequestParams> context)
        => ListarAgentesAsync(McpCaller.From(context));

    public Task<CallToolResult> ListarAgentesAsync(McpCaller caller) => McpToolResults.RunAsync(_logger, "listar_agentes", async () =>
    {
        var agents = await _agentService.GetAllAsync(caller.UserId);
        return _mapper.Map<List<AgentMcpSummaryInfo>>(agents);
    });

    [McpServerTool(Name = "obter_agente", Title = "Obter agente", ReadOnly = true, Idempotent = true)]
    [Description("Detalhes de um agente da conta: nome, slug, descricao, prompt de sistema, modelo, situacao, opcoes de coleta, nome do bot do Telegram e indicadores de credenciais (configurada ou nao, nunca o valor). Agente de outra conta ou inexistente responde 'Agente nao encontrado'.")]
    public Task<CallToolResult> ObterAgente(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente (veja listar_agentes).")] long agentId)
        => ObterAgenteAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ObterAgenteAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "obter_agente", async () =>
    {
        var agent = await _agentService.GetOwnedByIdAsync(agentId, caller.UserId)
            ?? throw new KeyNotFoundException(NotFoundMessage);
        return _mapper.Map<AgentMcpInfo>(agent);
    });

    [McpServerTool(Name = "criar_agente", Title = "Criar agente", ReadOnly = false, Idempotent = false)]
    [Description("Cria um agente na conta com nome, prompt de sistema e, opcionalmente, descricao, modelo (padrao gpt-4o), opcoes de coleta de dados do visitante e nome do bot do Telegram. O slug e gerado a partir do nome. Nao aceita chave de API nem tokens; esses campos sao recusados e nada e gravado. Devolve o agente criado.")]
    public Task<CallToolResult> CriarAgente(
        RequestContext<CallToolRequestParams> context,
        [Description("Nome do agente (obrigatorio, ate 260 caracteres).")] string name,
        [Description("Prompt de sistema que define o comportamento do agente (obrigatorio).")] string systemPrompt,
        [Description("Descricao curta exibida no painel e no chat.")] string? description = null,
        [Description("Modelo de chat, por exemplo gpt-4o ou gpt-4o-mini (padrao gpt-4o).")] string? chatModel = null,
        [Description("Pedir o nome do visitante antes da conversa (padrao false).")] bool? collectName = null,
        [Description("Pedir o e-mail do visitante antes da conversa (padrao false).")] bool? collectEmail = null,
        [Description("Pedir o telefone do visitante antes da conversa (padrao false).")] bool? collectPhone = null,
        [Description("Nome de exibicao do bot do Telegram (nao e o token).")] string? telegramBotName = null)
        => CriarAgenteAsync(McpCaller.From(context), new AgentMcpCreateInfo
        {
            Name = name,
            SystemPrompt = systemPrompt,
            Description = description,
            ChatModel = chatModel,
            CollectName = collectName,
            CollectEmail = collectEmail,
            CollectPhone = collectPhone,
            TelegramBotName = telegramBotName
        });

    public Task<CallToolResult> CriarAgenteAsync(McpCaller caller, AgentMcpCreateInfo info) => McpToolResults.RunAsync(_logger, "criar_agente", async () =>
    {
        // Monta o DTO do painel sem nenhum campo de segredo (openAIApiKey, telegramBotToken ficam nulos).
        var insert = new AgentInsertInfo
        {
            Name = info.Name?.Trim() ?? string.Empty,
            SystemPrompt = info.SystemPrompt ?? string.Empty,
            Description = info.Description,
            ChatModel = string.IsNullOrWhiteSpace(info.ChatModel) ? DefaultChatModel : info.ChatModel.Trim(),
            CollectName = info.CollectName ?? false,
            CollectEmail = info.CollectEmail ?? false,
            CollectPhone = info.CollectPhone ?? false,
            TelegramBotName = info.TelegramBotName
        };

        var validation = await _insertValidator.ValidateAsync(insert);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var agent = await _agentService.CreateAsync(insert, caller.UserId);
        return _mapper.Map<AgentMcpInfo>(agent);
    });

    [McpServerTool(Name = "atualizar_agente", Title = "Atualizar agente", ReadOnly = false, Idempotent = true)]
    [Description("Atualizacao parcial de um agente da conta: informe so os campos que mudam (nome, descricao, prompt de sistema, modelo, opcoes de coleta, nome do bot do Telegram); os demais sao mantidos. Renomear regenera o slug. Nao aceita chave de API nem tokens; esses campos sao recusados e nada e gravado. Credenciais, situacao (ativo/inativo) e Power BI nao sao alterados por esta ferramenta.")]
    public Task<CallToolResult> AtualizarAgente(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Novo nome (ate 260 caracteres).")] string? name = null,
        [Description("Nova descricao.")] string? description = null,
        [Description("Novo prompt de sistema (nao pode ficar vazio).")] string? systemPrompt = null,
        [Description("Novo modelo de chat.")] string? chatModel = null,
        [Description("Pedir o nome do visitante.")] bool? collectName = null,
        [Description("Pedir o e-mail do visitante.")] bool? collectEmail = null,
        [Description("Pedir o telefone do visitante.")] bool? collectPhone = null,
        [Description("Nome de exibicao do bot do Telegram (nao e o token).")] string? telegramBotName = null)
        => AtualizarAgenteAsync(McpCaller.From(context), agentId, new AgentProfileUpdateInfo
        {
            Name = name,
            Description = description,
            SystemPrompt = systemPrompt,
            ChatModel = chatModel,
            CollectName = collectName,
            CollectEmail = collectEmail,
            CollectPhone = collectPhone,
            TelegramBotName = telegramBotName
        });

    public Task<CallToolResult> AtualizarAgenteAsync(McpCaller caller, long agentId, AgentProfileUpdateInfo info) => McpToolResults.RunAsync(_logger, "atualizar_agente", async () =>
    {
        var agent = await _agentService.UpdateProfileAsync(agentId, info, caller.UserId);
        return _mapper.Map<AgentMcpInfo>(agent);
    });

    [McpServerTool(Name = "definir_agente_ativo", Title = "Ativar ou desativar agente", ReadOnly = false, Idempotent = true)]
    [Description("Ativa (active = true) ou desativa (active = false) um agente da conta. Um agente inativo deixa de atender no site, no widget, no Telegram e no WhatsApp. Nao exclui o agente: exclusao so pelo usuario no painel.")]
    public Task<CallToolResult> DefinirAgenteAtivo(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("true para ativar, false para desativar.")] bool active)
        => DefinirAgenteAtivoAsync(McpCaller.From(context), agentId, active);

    public Task<CallToolResult> DefinirAgenteAtivoAsync(McpCaller caller, long agentId, bool active) => McpToolResults.RunAsync(_logger, "definir_agente_ativo", async () =>
    {
        var agent = await _agentService.SetActiveAsync(agentId, active, caller.UserId);
        return _mapper.Map<AgentMcpInfo>(agent);
    });
}
