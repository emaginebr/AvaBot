using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Avachat.Application.Services;

public class TelegramService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ITelegramChatRepository<TelegramChat> _telegramChatRepo;
    private readonly IChatSessionRepository<ChatSession> _sessionRepo;
    private readonly ChatService _chatService;
    private readonly AgentService _agentService;
    private readonly ILogger<TelegramService> _logger;
    private readonly string _agentSlug;
    private readonly string _webhookUrl;
    private readonly string _webhookSecret;

    public TelegramService(
        ITelegramBotClient botClient,
        ITelegramChatRepository<TelegramChat> telegramChatRepo,
        IChatSessionRepository<ChatSession> sessionRepo,
        ChatService chatService,
        AgentService agentService,
        IConfiguration configuration,
        ILogger<TelegramService> logger)
    {
        _botClient = botClient;
        _telegramChatRepo = telegramChatRepo;
        _sessionRepo = sessionRepo;
        _chatService = chatService;
        _agentService = agentService;
        _logger = logger;
        _agentSlug = configuration["Telegram:AgentSlug"] ?? "";
        _webhookUrl = configuration["Telegram:WebhookUrl"] ?? "";
        _webhookSecret = configuration["Telegram:WebhookSecret"] ?? "";
    }

    public async Task ProcessUpdateAsync(Update update)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is not null && message.Text.StartsWith("/start"))
        {
            await HandleStartCommandAsync(message);
            return;
        }

        if (message.Text is null)
        {
            await SendMessageAsync(message.Chat.Id, "Desculpe, eu so consigo processar mensagens de texto.");
            return;
        }

        await HandleTextMessageAsync(message);
    }

    public async Task HandleStartCommandAsync(Message message)
    {
        try
        {
            var agent = await _agentService.GetBySlugAsync(_agentSlug);
            if (agent == null)
            {
                await SendMessageAsync(message.Chat.Id, "Bot nao configurado corretamente. Entre em contato com o suporte.");
                return;
            }

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
            await SendMessageAsync(message.Chat.Id, welcomeMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar /start para chat {ChatId}", message.Chat.Id);
            await SendMessageAsync(message.Chat.Id, "Ocorreu um erro. Por favor, tente novamente com /start.");
        }
    }

    public async Task HandleTextMessageAsync(Message message)
    {
        try
        {
            var telegramChat = await _telegramChatRepo.GetByChatIdAsync(message.Chat.Id);
            if (telegramChat == null)
            {
                await SendMessageAsync(message.Chat.Id, "Por favor, envie /start para iniciar uma conversa.");
                return;
            }

            var agent = await _agentService.GetBySlugAsync(_agentSlug);
            if (agent == null || agent.Status == 0)
            {
                await SendMessageAsync(message.Chat.Id, "O agente esta temporariamente indisponivel. Tente novamente mais tarde.");
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

            await SendMessageAsync(message.Chat.Id, fullResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do Telegram para chat {ChatId}", message.Chat.Id);
            await SendMessageAsync(message.Chat.Id, "Desculpe, ocorreu um erro ao processar sua mensagem. Tente novamente.");
        }
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        try
        {
            await _botClient.SendMessage(chatId, text, parseMode: ParseMode.Markdown);
        }
        catch (Exception)
        {
            // Fallback without Markdown if parsing fails
            await _botClient.SendMessage(chatId, text);
        }
    }

    public async Task<bool> SetupWebhookAsync()
    {
        await _botClient.SetWebhook(
            url: _webhookUrl,
            secretToken: _webhookSecret,
            allowedUpdates: [UpdateType.Message]);

        _logger.LogInformation("Webhook registrado com sucesso em {Url}", _webhookUrl);
        return true;
    }
}
