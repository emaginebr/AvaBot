using AutoMapper;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

/// <summary>
/// Leitura de sessoes e mensagens do painel e do MCP (feature 017, research R7): a validacao de dono e a
/// paginacao vivem aqui, para que os dois caminhos executem o mesmo codigo.
/// </summary>
public class SessionQueryService
{
    public const int DefaultSessionPageSize = 20;
    public const int MaxSessionPageSize = 100;
    public const int DefaultMessagePageSize = 50;
    public const int MaxMessagePageSize = 200;
    public const string AgentNotFoundMessage = "Agente nao encontrado";
    public const string SessionNotFoundMessage = "Sessao nao encontrada";

    public sealed record SessionPage(List<ChatSession> Items, int Total, int Page, int PageSize, Dictionary<long, int> MessageCounts);
    public sealed record MessagePage(List<ChatMessage> Items, int Total, int Page, int PageSize);

    private readonly IAgentRepository<Agent> _agentRepository;
    private readonly IChatSessionRepository<ChatSession> _sessionRepository;
    private readonly IChatMessageRepository<ChatMessage> _messageRepository;
    private readonly ITelegramChatRepository<TelegramChat> _telegramChatRepository;
    private readonly IMapper _mapper;

    public SessionQueryService(
        IAgentRepository<Agent> agentRepository,
        IChatSessionRepository<ChatSession> sessionRepository,
        IChatMessageRepository<ChatMessage> messageRepository,
        ITelegramChatRepository<TelegramChat> telegramChatRepository,
        IMapper mapper)
    {
        _agentRepository = agentRepository;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _telegramChatRepository = telegramChatRepository;
        _mapper = mapper;
    }

    /// <summary>Pagina de sessoes (entidades) para o painel; agente de outro dono responde como inexistente.</summary>
    public async Task<SessionPage> GetSessionPageAsync(long agentId, long ownerUserId, int page, int pageSize)
    {
        if (await _agentRepository.GetByIdAsync(agentId, ownerUserId) == null)
            throw new KeyNotFoundException(AgentNotFoundMessage);

        page = Math.Max(page, 1);
        pageSize = pageSize < 1 ? DefaultSessionPageSize : Math.Min(pageSize, MaxSessionPageSize);

        var sessions = await _sessionRepository.GetByAgentIdAsync(agentId, page, pageSize);
        var total = await _sessionRepository.CountByAgentIdAsync(agentId);

        var counts = new Dictionary<long, int>();
        foreach (var session in sessions)
            counts[session.ChatSessionId] = await _messageRepository.CountBySessionIdAsync(session.ChatSessionId);

        return new SessionPage(sessions, total, page, pageSize, counts);
    }

    /// <summary>Pagina de mensagens (entidades) para o painel; sessao de agente alheio responde como inexistente.</summary>
    public async Task<MessagePage> GetMessagePageAsync(long sessionId, long ownerUserId, int page, int pageSize)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session?.AgentId == null || await _agentRepository.GetByIdAsync(session.AgentId.Value, ownerUserId) == null)
            throw new KeyNotFoundException(SessionNotFoundMessage);

        page = Math.Max(page, 1);
        pageSize = pageSize < 1 ? DefaultMessagePageSize : Math.Min(pageSize, MaxMessagePageSize);

        var messages = await _messageRepository.GetBySessionIdAsync(sessionId, page, pageSize);
        var total = await _messageRepository.CountBySessionIdAsync(sessionId);

        return new MessagePage(messages, total, page, pageSize);
    }

    /// <summary>MCP: sessoes mais recentes primeiro, com canal (telegram ou web) e quantidade de mensagens.</summary>
    public async Task<PaginatedResult<ChatSessionMcpInfo>> GetSessionsAsync(long agentId, long ownerUserId, int page, int pageSize)
    {
        var data = await GetSessionPageAsync(agentId, ownerUserId, page, pageSize);
        var telegramSessions = await _telegramChatRepository.GetSessionIdsAsync(data.Items.Select(s => s.ChatSessionId));

        var items = data.Items.Select(s =>
        {
            var info = _mapper.Map<ChatSessionMcpInfo>(s);
            info.Channel = telegramSessions.Contains(s.ChatSessionId) ? "telegram" : "web";
            info.MessageCount = data.MessageCounts.TryGetValue(s.ChatSessionId, out var count) ? count : 0;
            return info;
        }).ToList();

        return new PaginatedResult<ChatSessionMcpInfo>
        {
            Items = items,
            Total = data.Total,
            Page = data.Page,
            MaxPage = MaxPageOf(data.Total, data.PageSize)
        };
    }

    /// <summary>MCP: mensagens em ordem cronologica com papel visitante/agente.</summary>
    public async Task<PaginatedResult<ChatMessageMcpInfo>> GetMessagesAsync(long sessionId, long ownerUserId, int page, int pageSize)
    {
        var data = await GetMessagePageAsync(sessionId, ownerUserId, page, pageSize);

        return new PaginatedResult<ChatMessageMcpInfo>
        {
            Items = _mapper.Map<List<ChatMessageMcpInfo>>(data.Items),
            Total = data.Total,
            Page = data.Page,
            MaxPage = MaxPageOf(data.Total, data.PageSize)
        };
    }

    private static int MaxPageOf(int total, int pageSize) =>
        total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
}
