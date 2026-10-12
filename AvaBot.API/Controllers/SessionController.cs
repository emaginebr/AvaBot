using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.DTO;
using AvaBot.Domain.Models;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.API.Controllers;

[Authorize]
[ApiController]
public class SessionController : ControllerBase
{
    private readonly IChatSessionRepository<ChatSession> _sessionRepo;
    private readonly IChatMessageRepository<ChatMessage> _messageRepo;
    private readonly AgentService _agentService;
    private readonly ChatService _chatService;
    private readonly SessionQueryService _sessionQuery;
    private readonly IMapper _mapper;

    public SessionController(
        IChatSessionRepository<ChatSession> sessionRepo,
        IChatMessageRepository<ChatMessage> messageRepo,
        AgentService agentService,
        ChatService chatService,
        SessionQueryService sessionQuery,
        IMapper mapper)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _agentService = agentService;
        _chatService = chatService;
        _sessionQuery = sessionQuery;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpPost("sessions/agents/{slug}")]
    public async Task<IActionResult> StartSession(string slug, [FromBody] ChatSessionStartInfo info)
    {
        try
        {
            var agent = await _agentService.GetBySlugAsync(slug);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            if (agent.Status == 0)
                return BadRequest(Result<object>.Failure("Agente temporariamente indisponivel"));

            // Validate required fields
            var errors = new List<string>();
            if (agent.CollectName && string.IsNullOrWhiteSpace(info.UserName))
                errors.Add("userName e obrigatorio para este agente");
            if (agent.CollectEmail && string.IsNullOrWhiteSpace(info.UserEmail))
                errors.Add("userEmail e obrigatorio para este agente");
            if (agent.CollectPhone && string.IsNullOrWhiteSpace(info.UserPhone))
                errors.Add("userPhone e obrigatorio para este agente");

            if (errors.Count > 0)
                return BadRequest(Result<object>.Failure("Campos obrigatorios nao preenchidos", errors.ToArray()));

            var session = await _chatService.CreateSessionAsync(
                agent.AgentId, info.UserName, info.UserEmail, info.UserPhone);

            var result = _mapper.Map<ChatSessionInfo>(session);
            return Created($"/sessions/{session.ChatSessionId}/messages",
                Result<ChatSessionInfo>.Success(result, "Sessao iniciada com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpGet("sessions/agents/{agentId:long}")]
    public async Task<IActionResult> GetSessions(long agentId, [FromQuery] int page = 1, [FromQuery] int maxPage = 20)
    {
        try
        {
            // Dono, paginacao e contagem ficam no SessionQueryService (compartilhado com o MCP, feature 017).
            var data = await _sessionQuery.GetSessionPageAsync(agentId, User.GetUserId(), page, maxPage);

            var items = data.Items.Select(s =>
            {
                var info = _mapper.Map<ChatSessionInfo>(s);
                info.MessageCount = data.MessageCounts.TryGetValue(s.ChatSessionId, out var count) ? count : 0;
                return info;
            }).ToList();

            var paginated = new PaginatedResult<ChatSessionInfo>
            {
                Items = items,
                Total = data.Total,
                Page = data.Page,
                MaxPage = data.PageSize
            };

            return Ok(Result<PaginatedResult<ChatSessionInfo>>.Success(paginated, "Sessoes listadas com sucesso"));
        }
        catch (KeyNotFoundException ex)
        {
            // Agente de outro dono responde como inexistente (FR-011).
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpGet("sessions/resume/{slug}")]
    public async Task<IActionResult> ResumeSession(string slug, [FromHeader(Name = "X-Resume-Token")] string? resumeToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resumeToken))
                return NotFound(Result<object>.Failure("Sessao nao encontrada"));

            var agent = await _agentService.GetBySlugAsync(slug);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            if (agent.Status == 0)
                return BadRequest(Result<object>.Failure("Agente temporariamente indisponivel"));

            var session = await _sessionRepo.GetByResumeTokenAsync(resumeToken);
            if (session == null || session.AgentId != agent.AgentId)
                return NotFound(Result<object>.Failure("Sessao nao encontrada"));

            var result = _mapper.Map<ChatSessionResumeInfo>(session);
            result.MessageCount = await _messageRepo.CountBySessionIdAsync(session.ChatSessionId);

            var lastMessages = await _messageRepo.GetLastBySessionIdAsync(session.ChatSessionId, 10);
            result.Messages = _mapper.Map<List<ChatMessageInfo>>(lastMessages);

            return Ok(Result<ChatSessionResumeInfo>.Success(result, "Sessao retomada com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpGet("sessions/{sessionId:long}/messages")]
    public async Task<IActionResult> GetMessages(long sessionId, [FromQuery] int page = 1, [FromQuery] int maxPage = 50)
    {
        try
        {
            // A sessao so e visivel se o agente dela for do usuario do token (FR-010); regra no SessionQueryService.
            var data = await _sessionQuery.GetMessagePageAsync(sessionId, User.GetUserId(), page, maxPage);

            var paginated = new PaginatedResult<ChatMessageInfo>
            {
                Items = _mapper.Map<List<ChatMessageInfo>>(data.Items),
                Total = data.Total,
                Page = data.Page,
                MaxPage = data.PageSize
            };

            return Ok(Result<PaginatedResult<ChatMessageInfo>>.Success(paginated, "Mensagens listadas com sucesso"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }
}
