using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>
/// Telegram e WhatsApp (contrato mcp-tools.md #22–#24). Nenhuma ferramenta le ou grava token, regenera o
/// segredo do webhook, pareia QR code ou desconecta: isso continua no painel (FR-015, FR-016).
/// </summary>
[McpServerToolType]
public sealed class ChannelTools
{
    private readonly AgentService _agentService;
    private readonly TelegramService _telegramService;
    private readonly WhatsappService _whatsappService;
    private readonly ILogger<ChannelTools> _logger;

    public ChannelTools(AgentService agentService, TelegramService telegramService, WhatsappService whatsappService, ILogger<ChannelTools> logger)
    {
        _agentService = agentService;
        _telegramService = telegramService;
        _whatsappService = whatsappService;
        _logger = logger;
    }

    /// <summary>Status devolvido quando o usuario ainda nao pareou o WhatsApp no painel.</summary>
    public const string NotStartedStatus = "NOT_STARTED";

    public static string MissingTokenMessage(string agentName) =>
        $"Este agente nao tem bot do Telegram configurado. Peca ao usuario para informar o token no painel, em Agentes > {agentName} > Telegram.";

    private async Task<Agent> ResolveAgentAsync(long agentId, McpCaller caller) =>
        await _agentService.GetOwnedByIdAsync(agentId, caller.UserId) ?? throw new KeyNotFoundException(AgentTools.NotFoundMessage);

    [McpServerTool(Name = "obter_telegram", Title = "Situacao do Telegram", ReadOnly = true, Idempotent = true)]
    [Description("Situacao do bot do Telegram de um agente: nome do bot, se o token foi configurado pelo usuario e se o webhook esta registrado (com a URL). Nunca devolve o token nem o segredo do webhook.")]
    public Task<CallToolResult> ObterTelegram(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ObterTelegramAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ObterTelegramAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "obter_telegram", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);

        // Sem token nao ha o que perguntar ao Telegram.
        if (string.IsNullOrEmpty(agent.TelegramBotToken))
            return new TelegramMcpInfo { BotName = agent.TelegramBotName, BotConfigured = false, WebhookRegistered = false };

        var info = await _telegramService.GetWebhookInfoAsync(agentId, caller.UserId);
        return new TelegramMcpInfo
        {
            BotName = agent.TelegramBotName,
            BotConfigured = true,
            WebhookRegistered = !string.IsNullOrEmpty(info.WebhookUrl),
            WebhookUrl = string.IsNullOrEmpty(info.WebhookUrl) ? null : info.WebhookUrl
        };
    });

    [McpServerTool(Name = "registrar_webhook_telegram", Title = "Registrar o webhook do Telegram", ReadOnly = false, Idempotent = true)]
    [Description("Registra (ou re-registra) o webhook do bot do Telegram do agente no endereco publico do AvaBot, usando o token ja informado pelo usuario. Nao existe ferramenta para informar o token nem para regenerar o segredo do webhook.")]
    public Task<CallToolResult> RegistrarWebhookTelegram(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => RegistrarWebhookTelegramAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> RegistrarWebhookTelegramAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "registrar_webhook_telegram", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);
        if (string.IsNullOrEmpty(agent.TelegramBotToken))
            throw new InvalidOperationException(MissingTokenMessage(agent.Name));

        var info = await _telegramService.SetupWebhookAsync(agentId, caller.UserId);
        return new TelegramMcpInfo
        {
            BotName = agent.TelegramBotName,
            BotConfigured = true,
            WebhookRegistered = info.IsConfigured,
            WebhookUrl = info.WebhookUrl
        };
    });

    [McpServerTool(Name = "obter_whatsapp", Title = "Situacao do WhatsApp", ReadOnly = true, Idempotent = true)]
    [Description("Situacao da conexao do WhatsApp do agente (status do WPP Connect e se esta conectado). O pareamento por QR code e a desconexao continuam sendo feitos pelo usuario no painel.")]
    public Task<CallToolResult> ObterWhatsapp(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ObterWhatsappAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ObterWhatsappAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "obter_whatsapp", async () =>
    {
        var agent = await ResolveAgentAsync(agentId, caller);

        // Sem sessao iniciada no painel nao ha o que consultar no WPP Connect.
        if (string.IsNullOrEmpty(agent.WhatsappToken))
            return new WhatsappMcpInfo { Status = NotStartedStatus, IsConnected = false };

        try
        {
            var status = await _whatsappService.GetStatusAsync(agent.Slug, caller.UserId);
            return new WhatsappMcpInfo { Status = status.Status, IsConnected = status.IsConnected };
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not OperationCanceledException)
        {
            throw new InvalidOperationException($"Nao foi possivel consultar o WhatsApp: {ex.Message}");
        }
    });
}
