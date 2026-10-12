using System.ComponentModel;
using FluentValidation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>
/// Power BI por agente (contrato mcp-tools.md #11–#21). Nunca le nem grava Directory ID, App ID ou segredo:
/// a saida e projetada para PowerBIMcpInfo e a gravacao de credencial nao existe aqui (FR-009, FR-014).
/// </summary>
[McpServerToolType]
public sealed class PowerBITools
{
    private readonly AgentService _agentService;
    private readonly PowerBIService _powerBIService;
    private readonly IValidator<PowerBIDatasetInsertInfo> _datasetValidator;
    private readonly ILogger<PowerBITools> _logger;

    public PowerBITools(AgentService agentService, PowerBIService powerBIService, IValidator<PowerBIDatasetInsertInfo> datasetValidator, ILogger<PowerBITools> logger)
    {
        _agentService = agentService;
        _powerBIService = powerBIService;
        _datasetValidator = datasetValidator;
        _logger = logger;
    }

    public static string MissingCredentialMessage(string agentName) =>
        $"O Power BI deste agente ainda nao tem credencial. Peca ao usuario para informar Directory ID, App ID e segredo no painel, em Agentes > {agentName} > Power BI, e tente de novo.";

    private async Task<Agent> ResolveAgentAsync(long agentId, McpCaller caller) =>
        await _agentService.GetOwnedByIdAsync(agentId, caller.UserId) ?? throw new KeyNotFoundException(AgentTools.NotFoundMessage);

    private static bool HasCredential(PowerBIConfigInfo config) => config.IsConfigured && config.HasClientSecret;

    private async Task<PowerBIConfigInfo> RequireCredentialAsync(Agent agent, McpCaller caller)
    {
        var config = await _powerBIService.GetConfigAsync(agent.Slug, caller.UserId);
        if (!HasCredential(config))
            throw new InvalidOperationException(MissingCredentialMessage(agent.Name));
        return config;
    }

    private async Task<PowerBIMcpInfo> ProjectAsync(Agent agent, McpCaller caller)
    {
        var config = await _powerBIService.GetConfigAsync(agent.Slug, caller.UserId);
        var datasets = await _powerBIService.GetDatasetsAsync(agent.Slug, caller.UserId);

        // Copia campo a campo: tenantId, clientId e clientSecretMasked ficam de fora de proposito.
        return new PowerBIMcpInfo
        {
            Enabled = config.Enabled,
            CredentialConfigured = HasCredential(config),
            LastTestAt = config.LastTestAt,
            LastTestSuccess = config.LastTestSuccess,
            LastTestMessage = config.LastTestMessage,
            Datasets = datasets
        };
    }

    [McpServerTool(Name = "obter_powerbi", Title = "Situacao do Power BI", ReadOnly = true, Idempotent = true)]
    [Description("Situacao do Power BI de um agente: ligado ou nao, credencial configurada ou nao, resultado do ultimo teste de conexao e os datasets vinculados com o status do schema. Nunca devolve Directory ID, App ID ou segredo; a credencial so pode ser informada pelo usuario no painel.")]
    public Task<CallToolResult> ObterPowerBI(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ObterPowerBIAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ObterPowerBIAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "obter_powerbi", async () =>
        await ProjectAsync(await ResolveAgentAsync(agentId, caller), caller));

    [McpServerTool(Name = "definir_powerbi_ativo", Title = "Ligar ou desligar o Power BI", ReadOnly = false, Idempotent = true)]
    [Description("Liga (enabled = true) ou desliga (enabled = false) o Power BI do agente. Ligar exige credencial informada pelo usuario no painel e ao menos um dataset com schema gerado.")]
    public Task<CallToolResult> DefinirPowerBIAtivo(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("true para ligar, false para desligar.")] bool enabled)
        => DefinirPowerBIAtivoAsync(McpCaller.From(context), agentId, enabled);

    public Task<CallToolResult> DefinirPowerBIAtivoAsync(McpCaller caller, long agentId, bool enabled) => McpToolResults.RunAsync(_logger, "definir_powerbi_ativo", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        if (enabled)
            await RequireCredentialAsync(agent, caller);

        await _powerBIService.SetEnabledAsync(agent.Slug, caller.UserId, enabled);
        return await ProjectAsync(agent, caller);
    });

    [McpServerTool(Name = "testar_conexao_powerbi", Title = "Testar a conexao com o Power BI", ReadOnly = true, Idempotent = true)]
    [Description("Roda o teste de conexao do Power BI com a credencial salva pelo usuario e devolve o resultado por etapa (autenticacao, workspaces, consulta), igual ao painel. Grava o resultado como ultimo teste.")]
    public Task<CallToolResult> TestarConexaoPowerBI(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => TestarConexaoPowerBIAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> TestarConexaoPowerBIAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "testar_conexao_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        await RequireCredentialAsync(agent, caller);
        return await _powerBIService.TestConnectionAsync(agent.Slug, caller.UserId);
    });

    [McpServerTool(Name = "listar_workspaces_powerbi", Title = "Listar workspaces do Power BI", ReadOnly = true, Idempotent = true)]
    [Description("Lista os workspaces que a credencial do agente enxerga no Power BI, cada um com os datasets disponiveis (workspaceId, datasetId, nome). Use vincular_dataset_powerbi com esses ids.")]
    public Task<CallToolResult> ListarWorkspacesPowerBI(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ListarWorkspacesPowerBIAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ListarWorkspacesPowerBIAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "listar_workspaces_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        await RequireCredentialAsync(agent, caller);
        return await _powerBIService.ListWorkspacesAsync(agent.Slug, caller.UserId);
    });

    [McpServerTool(Name = "listar_datasets_powerbi", Title = "Listar datasets vinculados", ReadOnly = true, Idempotent = true)]
    [Description("Lista os datasets ja vinculados ao agente, com nome, descricao, status do schema e contagens de tabelas, colunas e medidas.")]
    public Task<CallToolResult> ListarDatasetsPowerBI(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ListarDatasetsPowerBIAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ListarDatasetsPowerBIAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "listar_datasets_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _powerBIService.GetDatasetsAsync(agent.Slug, caller.UserId);
    });

    [McpServerTool(Name = "vincular_dataset_powerbi", Title = "Vincular dataset", ReadOnly = false, Idempotent = false)]
    [Description("Vincula um dataset do Power BI ao agente, com nome e descricao de exibicao. Depois use gerar_schema_powerbi para o agente poder consulta-lo.")]
    public Task<CallToolResult> VincularDatasetPowerBI(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do workspace (de listar_workspaces_powerbi).")] string workspaceId,
        [Description("Id do dataset no Power BI (de listar_workspaces_powerbi).")] string datasetId,
        [Description("Nome de exibicao do dataset (ate 120 caracteres).")] string name,
        [Description("Descricao do que o dataset contem, usada pelo agente para escolher o dataset certo.")] string? description = null)
        => VincularDatasetPowerBIAsync(McpCaller.From(context), agentId, new PowerBIDatasetInsertInfo { WorkspaceId = workspaceId, DatasetId = datasetId, Name = name, Description = description });

    public Task<CallToolResult> VincularDatasetPowerBIAsync(McpCaller caller, long agentId, PowerBIDatasetInsertInfo info) => McpToolResults.RunAsync(_logger, "vincular_dataset_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        await RequireCredentialAsync(agent, caller);

        var validation = await _datasetValidator.ValidateAsync(info);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        return await _powerBIService.CreateDatasetAsync(agent.Slug, caller.UserId, info);
    });

    [McpServerTool(Name = "atualizar_dataset_powerbi", Title = "Atualizar dataset", ReadOnly = false, Idempotent = true)]
    [Description("Altera o nome e/ou a descricao de um dataset vinculado; campo ausente mantem o valor. Workspace e dataset do Power BI nao mudam.")]
    public Task<CallToolResult> AtualizarDatasetPowerBI(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do dataset vinculado (powerBIDatasetId).")] long powerBIDatasetId,
        [Description("Novo nome de exibicao.")] string? name = null,
        [Description("Nova descricao.")] string? description = null)
        => AtualizarDatasetPowerBIAsync(McpCaller.From(context), agentId, powerBIDatasetId, name, description);

    public Task<CallToolResult> AtualizarDatasetPowerBIAsync(McpCaller caller, long agentId, long powerBIDatasetId, string? name, string? description) => McpToolResults.RunAsync(_logger, "atualizar_dataset_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        var datasets = await _powerBIService.GetDatasetsAsync(agent.Slug, caller.UserId);
        var current = datasets.FirstOrDefault(d => d.PowerBIDatasetId == powerBIDatasetId)
            ?? throw new KeyNotFoundException("Dataset nao encontrado");

        var info = new PowerBIDatasetInsertInfo
        {
            WorkspaceId = current.WorkspaceId,
            DatasetId = current.DatasetId,
            Name = name ?? current.Name,
            Description = description ?? current.Description
        };

        return await _powerBIService.UpdateDatasetAsync(agent.Slug, caller.UserId, powerBIDatasetId, info);
    });

    [McpServerTool(Name = "desvincular_dataset_powerbi", Title = "Desvincular dataset", ReadOnly = false, Idempotent = false, Destructive = true)]
    [Description("Remove o vinculo de um dataset com o agente (o dataset continua existindo no Power BI). Se nao sobrar dataset utilizavel, o Power BI do agente e desligado.")]
    public Task<CallToolResult> DesvincularDatasetPowerBI(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do dataset vinculado (powerBIDatasetId).")] long powerBIDatasetId)
        => DesvincularDatasetPowerBIAsync(McpCaller.From(context), agentId, powerBIDatasetId);

    public Task<CallToolResult> DesvincularDatasetPowerBIAsync(McpCaller caller, long agentId, long powerBIDatasetId) => McpToolResults.RunAsync(_logger, "desvincular_dataset_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        var message = await _powerBIService.DeleteDatasetAsync(agent.Slug, caller.UserId, powerBIDatasetId);
        return new McpMessageInfo { Message = message };
    });

    [McpServerTool(Name = "gerar_schema_powerbi", Title = "Gerar schema do dataset", ReadOnly = false, Idempotent = true)]
    [Description("Le tabelas, colunas e medidas do dataset no Power BI e grava o schema que o agente usa para montar consultas. Preserva as descricoes ja escritas pelo usuario. Pode levar ate um minuto.")]
    public Task<CallToolResult> GerarSchemaPowerBI(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do dataset vinculado (powerBIDatasetId).")] long powerBIDatasetId)
        => GerarSchemaPowerBIAsync(McpCaller.From(context), agentId, powerBIDatasetId);

    public Task<CallToolResult> GerarSchemaPowerBIAsync(McpCaller caller, long agentId, long powerBIDatasetId) => McpToolResults.RunAsync(_logger, "gerar_schema_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        await RequireCredentialAsync(agent, caller);
        return await _powerBIService.GenerateSchemaAsync(agent.Slug, caller.UserId, powerBIDatasetId);
    });

    [McpServerTool(Name = "obter_schema_powerbi", Title = "Obter schema do dataset", ReadOnly = true, Idempotent = true)]
    [Description("Devolve o schema gravado de um dataset: tabelas, colunas (com tipo) e medidas, cada uma com a descricao vinda do Power BI e a descricao escrita pelo usuario.")]
    public Task<CallToolResult> ObterSchemaPowerBI(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do dataset vinculado (powerBIDatasetId).")] long powerBIDatasetId)
        => ObterSchemaPowerBIAsync(McpCaller.From(context), agentId, powerBIDatasetId);

    public Task<CallToolResult> ObterSchemaPowerBIAsync(McpCaller caller, long agentId, long powerBIDatasetId) => McpToolResults.RunAsync(_logger, "obter_schema_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        return await _powerBIService.GetSchemaAsync(agent.Slug, caller.UserId, powerBIDatasetId);
    });

    [McpServerTool(Name = "descrever_schema_powerbi", Title = "Descrever tabelas, colunas e medidas", ReadOnly = false, Idempotent = true)]
    [Description("Grava descricoes em portugues para tabelas, colunas e medidas do schema de um dataset, para o agente entender os dados. Cada item tem kind (table, column ou measure), table (nome da tabela), name (nome da coluna ou medida; vazio para tabela) e userDescription (texto, ou vazio para apagar). Devolve o schema atualizado.")]
    public Task<CallToolResult> DescreverSchemaPowerBI(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do dataset vinculado (powerBIDatasetId).")] long powerBIDatasetId,
        [Description("Lista de descricoes: { kind: table|column|measure, table, name?, userDescription }.")] List<PowerBISchemaDescriptionItemInfo> items)
        => DescreverSchemaPowerBIAsync(McpCaller.From(context), agentId, powerBIDatasetId, items);

    public Task<CallToolResult> DescreverSchemaPowerBIAsync(McpCaller caller, long agentId, long powerBIDatasetId, List<PowerBISchemaDescriptionItemInfo> items) => McpToolResults.RunAsync(_logger, "descrever_schema_powerbi", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        var update = new PowerBISchemaDescriptionUpdateInfo { Items = items ?? new List<PowerBISchemaDescriptionItemInfo>() };
        return await _powerBIService.UpdateSchemaDescriptionsAsync(agent.Slug, caller.UserId, powerBIDatasetId, update);
    });
}
