using System.Security.Cryptography;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AvaBot.Application.Services;

public class TelegramService
{
    private const string WEBHOOK_BASE_URL = "https://avabot.net/api/telegram";

    private readonly ITelegramChatRepository<TelegramChat> _telegramChatRepo;
    private readonly IChatSessionRepository<ChatSession> _sessionRepo;
    private readonly IAgentRepository<Agent> _agentRepo;
    private readonly ChatService _chatService;
    private readonly AgentService _agentService;
    private readonly ILogger<TelegramService> _logger;

    public TelegramService(
        ITelegramChatRepository<TelegramChat> telegramChatRepo,
        IChatSessionRepository<ChatSession> sessionRepo,
        IAgentRepository<Agent> agentRepo,
        ChatService chatService,
        AgentService agentService,
        ILogger<TelegramService> logger)
    {
        _telegramChatRepo = telegramChatRepo;
        _sessionRepo = sessionRepo;
        _agentRepo = agentRepo;
        _chatService = chatService;
        _agentService = agentService;
        _logger = logger;
    }

    public async Task ProcessUpdateAsync(Agent agent, Update update)
    {
        var botClient = CreateBotClient(agent.TelegramBotToken!);

        if (update.Message is not { } message)
            return;

        if (message.Text is not null && message.Text.StartsWith("/start"))
        {
            await HandleStartCommandAsync(agent, botClient, message);
            return;
        }

        if (message.Text is null)
        {
            await SendMessageAsync(botClient, message.Chat.Id, "Desculpe, eu so consigo processar mensagens de texto.");
            return;
        }

        await HandleTextMessageAsync(agent, botClient, message);
    }

    private async Task HandleStartCommandAsync(Agent agent, ITelegramBotClient botClient, Message message)
    {
        try
        {
            var session = await _chatService.CreateSessionAsync(
                agent.AgentId,
                message.From?.FirstName,
                null,
                null);

            var existingChat = await _telegramChatRepo.GetByChatIdAsync(message.Chat.Id);
            if (existingChat != null)
            {
                existingChat.ChatSessionId = session.ChatSessionId;
                existingChat.TelegramUsername = message.From?.Username;
                existingChat.TelegramFirstName = message.From?.FirstName;
                await _telegramChatRepo.UpdateAsync(existingChat);
            }
            else
            {
                var telegramChat = new TelegramChat
                {
                    TelegramChatId = message.Chat.Id,
                    AgentId = agent.AgentId,
                    ChatSessionId = session.ChatSessionId,
                    TelegramUsername = message.From?.Username,
                    TelegramFirstName = message.From?.FirstName
                };
                await _telegramChatRepo.CreateAsync(telegramChat);
            }

            var welcomeMessage = $"Ola{(message.From?.FirstName != null ? $", {message.From.FirstName}" : "")}! "
                + $"Eu sou o assistente {agent.Name}. Como posso ajudar voce hoje?";
            await SendMessageAsync(botClient, message.Chat.Id, welcomeMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar /start para chat {ChatId}", message.Chat.Id);
            await SendMessageAsync(botClient, message.Chat.Id, "Ocorreu um erro. Por favor, tente novamente com /start.");
        }
    }

    private async Task HandleTextMessageAsync(Agent agent, ITelegramBotClient botClient, Message message)
    {
        try
        {
            var telegramChat = await _telegramChatRepo.GetByChatIdAsync(message.Chat.Id);
            if (telegramChat == null)
            {
                await SendMessageAsync(botClient, message.Chat.Id, "Por favor, envie /start para iniciar uma conversa.");
                return;
            }

            if (agent.Status == 0)
            {
                await SendMessageAsync(botClient, message.Chat.Id, "O agente esta temporariamente indisponivel. Tente novamente mais tarde.");
                return;
            }

            var fullResponse = string.Empty;
            await foreach (var token in _chatService.ProcessMessageAsync(
                agent.AgentId,
                telegramChat.ChatSessionId,
                agent.ChatModel,
                agent.SystemPrompt,
                message.Text!))
            {
                fullResponse += token;
            }

            await SendMessageAsync(botClient, message.Chat.Id, fullResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do Telegram para chat {ChatId}", message.Chat.Id);
            await SendMessageAsync(botClient, message.Chat.Id, "Desculpe, ocorreu um erro ao processar sua mensagem. Tente novamente.");
        }
    }

    private static async Task SendMessageAsync(ITelegramBotClient botClient, long chatId, string text)
    {
        try
        {
            await botClient.SendMessage(chatId, text, parseMode: ParseMode.Markdown);
        }
        catch (Exception)
        {
            await botClient.SendMessage(chatId, text);
        }
    }

    public async Task<TelegramWebhookInfo> SetupWebhookAsync(long agentId)
    {
        var agent = await _agentRepo.GetByIdAsync(agentId)
            ?? throw new KeyNotFoundException($"Agente {agentId} nao encontrado");

        if (string.IsNullOrEmpty(agent.TelegramBotToken))
            throw new InvalidOperationException("Agente nao possui TelegramBotToken configurado");

        var botClient = CreateBotClient(agent.TelegramBotToken);
        var webhookUrl = $"{WEBHOOK_BASE_URL}/{agent.Slug}/webhook";

        await botClient.SetWebhook(
            url: webhookUrl,
            secretToken: agent.TelegramWebhookSecret,
            allowedUpdates: [UpdateType.Message]);

        _logger.LogInformation("Webhook registrado para agente {AgentId} em {Url}", agentId, webhookUrl);

        return new TelegramWebhookInfo
        {
            AgentId = agent.AgentId,
            AgentSlug = agent.Slug,
            WebhookUrl = webhookUrl,
            IsConfigured = true
        };
    }

    public async Task<TelegramWebhookInfo> GetWebhookInfoAsync(long agentId)
    {
        var agent = await _agentRepo.GetByIdAsync(agentId)
            ?? throw new KeyNotFoundException($"Agente {agentId} nao encontrado");

        if (string.IsNullOrEmpty(agent.TelegramBotToken))
            throw new InvalidOperationException("Agente nao possui TelegramBotToken configurado");

        var botClient = CreateBotClient(agent.TelegramBotToken);
        var webhookInfo = await botClient.GetWebhookInfo();

        return new TelegramWebhookInfo
        {
            AgentId = agent.AgentId,
            AgentSlug = agent.Slug,
            WebhookUrl = webhookInfo.Url,
            IsConfigured = !string.IsNullOrEmpty(webhookInfo.Url)
        };
    }

    public async Task<TelegramWebhookInfo> RegenerateWebhookSecretAsync(long agentId)
    {
        var agent = await _agentRepo.GetByIdAsync(agentId)
            ?? throw new KeyNotFoundException($"Agente {agentId} nao encontrado");

        if (string.IsNullOrEmpty(agent.TelegramBotToken))
            throw new InvalidOperationException("Agente nao possui TelegramBotToken configurado");

        agent.TelegramWebhookSecret = GenerateWebhookSecret();
        await _agentRepo.UpdateAsync(agent);

        return await SetupWebhookAsync(agentId);
    }

    public static string GenerateWebhookSecret()
    {
        return RandomNumberGenerator.GetHexString(32);
    }

    private static ITelegramBotClient CreateBotClient(string botToken)
    {
        return new TelegramBotClient(botToken);
    }
}
