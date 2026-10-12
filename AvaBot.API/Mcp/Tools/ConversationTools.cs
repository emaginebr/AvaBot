using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.Application.Services;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>Conversas, logs de consulta BI e pergunta de teste (contrato mcp-tools.md #7–#10).</summary>
[McpServerToolType]
public sealed class ConversationTools
{
    public const string QuestionRequiredMessage = "O parametro 'question' e obrigatorio";

    private readonly SessionQueryService _sessionQuery;
    private readonly AgentService _agentService;
    private readonly PowerBIService _powerBIService;
    private readonly ChatService _chatService;
    private readonly ILogger<ConversationTools> _logger;

    public ConversationTools(
        SessionQueryService sessionQuery,
        AgentService agentService,
        PowerBIService powerBIService,
        ChatService chatService,
        ILogger<ConversationTools> logger)
    {
        _sessionQuery = sessionQuery;
        _agentService = agentService;
        _powerBIService = powerBIService;
        _chatService = chatService;
        _logger = logger;
    }

    [McpServerTool(Name = "listar_sessoes", Title = "Listar sessoes de conversa", ReadOnly = true, Idempotent = true)]
    [Description("Lista as sessoes de conversa de um agente, mais recentes primeiro, paginadas: data de inicio e fim, canal (web ou telegram), dados coletados do visitante e quantidade de mensagens. Use listar_mensagens com o chatSessionId para ler a conversa. Devolve { items, total, page, maxPage }.")]
    public Task<CallToolResult> ListarSessoes(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Pagina, a partir de 1 (padrao 1).")] int page = 1,
        [Description("Itens por pagina (padrao 20, maximo 100).")] int pageSize = SessionQueryService.DefaultSessionPageSize)
        => ListarSessoesAsync(McpCaller.From(context), agentId, page, pageSize);

    public Task<CallToolResult> ListarSessoesAsync(McpCaller caller, long agentId, int page, int pageSize) =>
        McpToolResults.RunAsync(_logger, "listar_sessoes", () => _sessionQuery.GetSessionsAsync(agentId, caller.UserId, page, pageSize));

    [McpServerTool(Name = "listar_mensagens", Title = "Listar mensagens de uma sessao", ReadOnly = true, Idempotent = true)]
    [Description("Mensagens de uma sessao de conversa em ordem cronologica, paginadas, com papel (visitante ou agente), conteudo e horario. Sessao de outra conta responde 'Sessao nao encontrada'.")]
    public Task<CallToolResult> ListarMensagens(
        RequestContext<CallToolRequestParams> context,
        [Description("Id da sessao (chatSessionId de listar_sessoes).")] long sessionId,
        [Description("Pagina, a partir de 1 (padrao 1).")] int page = 1,
        [Description("Itens por pagina (padrao 50, maximo 200).")] int pageSize = SessionQueryService.DefaultMessagePageSize)
        => ListarMensagensAsync(McpCaller.From(context), sessionId, page, pageSize);

    public Task<CallToolResult> ListarMensagensAsync(McpCaller caller, long sessionId, int page, int pageSize) =>
        McpToolResults.RunAsync(_logger, "listar_mensagens", () => _sessionQuery.GetMessagesAsync(sessionId, caller.UserId, page, pageSize));

    [McpServerTool(Name = "listar_consultas_bi", Title = "Listar consultas Power BI", ReadOnly = true, Idempotent = true)]
    [Description("Logs das consultas Power BI executadas pelo agente, mais recentes primeiro, paginados: pergunta do visitante, dataset, consulta DAX gerada, duracao, linhas retornadas, situacao e erro. Inclui as consultas feitas por perguntas de teste. Filtre por status = success, error ou timeout para ver so um tipo.")]
    public Task<CallToolResult> ListarConsultasBi(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Pagina, a partir de 1 (padrao 1).")] int page = 1,
        [Description("Itens por pagina (padrao 20, maximo 100).")] int pageSize = 20,
        [Description("Filtro de situacao: success, error ou timeout. Vazio traz todas.")] string? status = null)
        => ListarConsultasBiAsync(McpCaller.From(context), agentId, page, pageSize, status);

    public Task<CallToolResult> ListarConsultasBiAsync(McpCaller caller, long agentId, int page, int pageSize, string? status) =>
        McpToolResults.RunAsync(_logger, "listar_consultas_bi", async () =>
        {
            var parsed = PowerBIQueryStatusParser.ParseOrThrow(status);
            var agent = await _agentService.GetOwnedByIdAsync(agentId, caller.UserId)
                ?? throw new KeyNotFoundException(AgentTools.NotFoundMessage);
            return await _powerBIService.GetQueryLogsAsync(agent.Slug, caller.UserId, page, pageSize, parsed);
        });

    [McpServerTool(Name = "testar_agente", Title = "Fazer uma pergunta de teste ao agente", ReadOnly = true, Idempotent = false)]
    [Description("Faz uma pergunta de teste ao agente, igual ao teste do painel, e devolve a resposta com o rastro completo: texto buscado na base, consultas Power BI executadas e as rodadas do modelo. Nao cria sessao nem mensagem no historico de conversas, mas as consultas Power BI do teste entram no log de consultas do agente. Aceita um historico opcional de mensagens anteriores (role user ou assistant). Usa a chave de API ja configurada no agente ou no servidor.")]
    public Task<CallToolResult> TestarAgente(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Pergunta do visitante.")] string question,
        [Description("Mensagens anteriores da conversa, em ordem, cada uma com role (user ou assistant) e content.")] List<AgentTestMessageInfo>? history = null)
        => TestarAgenteAsync(McpCaller.From(context), agentId, question, history);

    public Task<CallToolResult> TestarAgenteAsync(McpCaller caller, long agentId, string question, List<AgentTestMessageInfo>? history) =>
        McpToolResults.RunAsync(_logger, "testar_agente", async () =>
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException(QuestionRequiredMessage);

            ValidateHistory(history);

            var agent = await _agentService.GetOwnedByIdAsync(agentId, caller.UserId)
                ?? throw new KeyNotFoundException(AgentTools.NotFoundMessage);

            return await _chatService.TestMessageAsync(agentId, agent.ChatModel, agent.SystemPrompt, question, history);
        });

    // Mesmas mensagens de AgentController.TestQuestion (FR-018).
    public static void ValidateHistory(List<AgentTestMessageInfo>? history)
    {
        if (history == null) return;

        for (var i = 0; i < history.Count; i++)
        {
            var item = history[i];

            if (item.Role is not ("user" or "assistant"))
                throw new ArgumentException($"Historico invalido: item {i + 1} tem role '{item.Role}' (use 'user' ou 'assistant')");

            if (string.IsNullOrWhiteSpace(item.Content))
                throw new ArgumentException($"Historico invalido: item {i + 1} esta vazio");
        }
    }
}
