using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;

namespace Avachat.Application.Services;

public class ChatService
{
    private readonly SearchService _searchService;
    private readonly IOpenAIService _openAIService;
    private readonly IChatSessionRepository<ChatSession> _sessionRepository;
    private readonly IChatMessageRepository<ChatMessage> _messageRepository;
    private readonly int _maxHistoryMessages;

    public ChatService(
        SearchService searchService,
        IOpenAIService openAIService,
        IChatSessionRepository<ChatSession> sessionRepository,
        IChatMessageRepository<ChatMessage> messageRepository,
        IConfiguration configuration)
    {
        _searchService = searchService;
        _openAIService = openAIService;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _maxHistoryMessages = int.TryParse(configuration["Chat:MaxHistoryMessages"], out var maxHistory) ? maxHistory : 20;
    }

    public async Task<ChatSession> CreateSessionAsync(long agentId, string? userName, string? userEmail, string? userPhone)
    {
        var session = new ChatSession
        {
            AgentId = agentId,
            UserName = userName,
            UserEmail = userEmail,
            UserPhone = userPhone
        };
        return await _sessionRepository.CreateAsync(session);
    }

    public async Task<ChatMessage> SaveMessageAsync(long sessionId, SenderType senderType, string content)
    {
        var message = new ChatMessage
        {
            ChatSessionId = sessionId,
            SenderType = senderType,
            Content = content
        };
        return await _messageRepository.CreateAsync(message);
    }

    public async IAsyncEnumerable<string> ProcessMessageAsync(
        long agentId,
        long sessionId,
        string systemPrompt,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Save user message
        await SaveMessageAsync(sessionId, SenderType.User, userMessage);

        // Search for relevant chunks
        var chunks = await _searchService.SearchAsync(agentId, userMessage);

        // Build context
        var context = chunks.Count > 0
            ? "Contexto relevante da base de conhecimento:\n\n" + string.Join("\n\n---\n\n", chunks)
            : "";

        // Get recent history
        var recentMessages = await _messageRepository.GetRecentBySessionIdAsync(sessionId, _maxHistoryMessages);

        // Build messages list
        var messages = new List<ChatCompletionMessage>();

        foreach (var msg in recentMessages)
        {
            // Skip the message we just saved (last user message)
            if (msg.ChatMessageId == 0) continue;
            messages.Add(new ChatCompletionMessage
            {
                Role = msg.SenderType == SenderType.User ? "user" : "assistant",
                Content = msg.Content
            });
        }

        // Add current user message with context
        var augmentedMessage = string.IsNullOrEmpty(context)
            ? userMessage
            : $"{userMessage}\n\n{context}";

        messages.Add(new ChatCompletionMessage { Role = "user", Content = augmentedMessage });

        // Build system prompt with grounding instruction
        var fullSystemPrompt = systemPrompt + "\n\nIMPORTANTE: Responda SOMENTE com base no contexto fornecido. Se nao encontrar informacao relevante no contexto, informe que nao possui essa informacao.";

        // Stream response
        var fullResponse = string.Empty;
        await foreach (var token in _openAIService.StreamChatCompletionAsync(fullSystemPrompt, messages, cancellationToken))
        {
            fullResponse += token;
            yield return token;
        }

        // Save assistant response
        await SaveMessageAsync(sessionId, SenderType.Assistant, fullResponse);
    }

    public async Task EndSessionAsync(long sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session != null)
        {
            session.EndedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
        }
    }
}
