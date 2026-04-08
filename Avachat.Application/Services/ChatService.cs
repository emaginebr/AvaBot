using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Avachat.Application.Services;

public class ChatService
{
    private readonly SearchService _searchService;
    private readonly IOpenAIService _openAIService;
    private readonly IChatSessionRepository<ChatSession> _sessionRepository;
    private readonly IChatMessageRepository<ChatMessage> _messageRepository;
    private readonly ILogger<ChatService> _logger;
    private readonly int _maxHistoryMessages;

    public ChatService(
        SearchService searchService,
        IOpenAIService openAIService,
        IChatSessionRepository<ChatSession> sessionRepository,
        IChatMessageRepository<ChatMessage> messageRepository,
        IConfiguration configuration,
        ILogger<ChatService> logger)
    {
        _searchService = searchService;
        _openAIService = openAIService;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _logger = logger;
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
        // Get session data (user info)
        var session = await _sessionRepository.GetByIdAsync(sessionId);

        // Get recent history BEFORE saving (to avoid duplicate)
        var recentMessages = await _messageRepository.GetRecentBySessionIdAsync(sessionId, _maxHistoryMessages);

        // Save user message
        await SaveMessageAsync(sessionId, SenderType.User, userMessage);

        // Search for relevant chunks
        var chunks = await _searchService.SearchAsync(agentId, userMessage);

        // Build context
        var context = chunks.Count > 0
            ? "Contexto relevante da base de conhecimento:\n\n" + string.Join("\n\n---\n\n", chunks)
            : "";

        // Build user info context
        var userInfo = BuildUserInfoContext(session);

        // Build system prompt with grounding instruction and user info
        var fullSystemPrompt = systemPrompt
            + "\n\nIMPORTANTE: Responda SOMENTE com base no contexto fornecido. Se nao encontrar informacao relevante no contexto, informe que nao possui essa informacao.";

        if (!string.IsNullOrEmpty(userInfo))
            fullSystemPrompt += $"\n\nINFORMACOES DO USUARIO NA SESSAO:\n{userInfo}";

        // Build messages list from history
        var messages = new List<ChatCompletionMessage>();

        foreach (var msg in recentMessages)
        {
            messages.Add(new ChatCompletionMessage
            {
                Role = msg.SenderType == SenderType.User ? "user" : "assistant",
                Content = msg.Content
            });
        }

        // Add RAG context as assistant knowledge (separate from user message)
        if (!string.IsNullOrEmpty(context))
        {
            messages.Add(new ChatCompletionMessage { Role = "assistant", Content = context });
        }

        // Add current user message (clean, without context mixed in)
        messages.Add(new ChatCompletionMessage { Role = "user", Content = userMessage });

        // Log
        _logger.LogInformation(
            "\n\n" +
            "╔══════════════════════════════════════════════════════════╗\n" +
            "║                    OPENAI REQUEST                       ║\n" +
            "╚══════════════════════════════════════════════════════════╝\n\n" +
            "┌─── SYSTEM PROMPT ───────────────────────────────────────┐\n" +
            "{SystemPrompt}\n" +
            "└─────────────────────────────────────────────────────────┘",
            fullSystemPrompt);

        _logger.LogInformation(
            "\n┌─── RAG CONTEXT ({ChunkCount} chunks) ─────────────────────────────┐\n" +
            "{Context}\n" +
            "└─────────────────────────────────────────────────────────┘",
            chunks.Count,
            chunks.Count > 0 ? string.Join("\n---\n", chunks.Select((c, i) => $"[Chunk {i + 1}] {(c.Length > 200 ? c[..200] + "..." : c)}")) : "(nenhum)");

        var historyMessages = messages.Take(messages.Count - 1).ToList();
        _logger.LogInformation(
            "\n┌─── HISTORICO ({MessageCount} mensagens) ──────────────────────────┐",
            historyMessages.Count);
        for (int i = 0; i < historyMessages.Count; i++)
        {
            var role = historyMessages[i].Role == "user" ? "USUARIO" : "ASSISTENTE";
            var preview = historyMessages[i].Content.Length > 300 ? historyMessages[i].Content[..300] + "..." : historyMessages[i].Content;
            _logger.LogInformation("  [{Index}] [{Role}] {Content}", i, role, preview);
        }
        _logger.LogInformation("└─────────────────────────────────────────────────────────┘");

        _logger.LogInformation(
            "\n┌─── ULTIMA MENSAGEM DO USUARIO ─────────────────────────────┐\n" +
            "{UserMessage}\n" +
            "└─────────────────────────────────────────────────────────┘\n",
            userMessage);

        // Stream response
        var fullResponse = string.Empty;
        await foreach (var token in _openAIService.StreamChatCompletionAsync(fullSystemPrompt, messages, cancellationToken))
        {
            fullResponse += token;
            yield return token;
        }

        // Log response
        _logger.LogInformation(
            "\n┌─── OPENAI RESPONSE ──────────────────────────────────────┐\n" +
            "{Response}\n" +
            "└─────────────────────────────────────────────────────────┘\n",
            fullResponse);

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

    private static string BuildUserInfoContext(ChatSession? session)
    {
        if (session == null) return "";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(session.UserName))
            parts.Add($"- Nome: {session.UserName}");
        if (!string.IsNullOrWhiteSpace(session.UserEmail))
            parts.Add($"- Email: {session.UserEmail}");
        if (!string.IsNullOrWhiteSpace(session.UserPhone))
            parts.Add($"- Telefone: {session.UserPhone}");

        return parts.Count > 0 ? string.Join("\n", parts) : "";
    }
}
