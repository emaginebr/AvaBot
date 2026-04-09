using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Avachat.DTO;
using Avachat.Domain.Models;
using Avachat.Application.Services;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.API.Controllers;

[Authorize]
[ApiController]
public class SessionController : ControllerBase
{
    private readonly IChatSessionRepository<ChatSession> _sessionRepo;
    private readonly IChatMessageRepository<ChatMessage> _messageRepo;
    private readonly AgentService _agentService;
    private readonly ChatService _chatService;
    private readonly IMapper _mapper;

    public SessionController(
        IChatSessionRepository<ChatSession> sessionRepo,
        IChatMessageRepository<ChatMessage> messageRepo,
        AgentService agentService,
        ChatService chatService,
        IMapper mapper)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _agentService = agentService;
        _chatService = chatService;
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
            maxPage = Math.Min(maxPage, 100);
            var sessions = await _sessionRepo.GetByAgentIdAsync(agentId, page, maxPage);
            var total = await _sessionRepo.CountByAgentIdAsync(agentId);

            var items = new List<ChatSessionInfo>();
            foreach (var s in sessions)
            {
                var info = _mapper.Map<ChatSessionInfo>(s);
                info.MessageCount = await _messageRepo.CountBySessionIdAsync(s.ChatSessionId);
                items.Add(info);
            }

            var paginated = new PaginatedResult<ChatSessionInfo>
            {
                Items = items,
                Total = total,
                Page = page,
                MaxPage = maxPage
            };

            return Ok(Result<PaginatedResult<ChatSessionInfo>>.Success(paginated, "Sessoes listadas com sucesso"));
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
            maxPage = Math.Min(maxPage, 200);
            var messages = await _messageRepo.GetBySessionIdAsync(sessionId, page, maxPage);
            var total = await _messageRepo.CountBySessionIdAsync(sessionId);

            var items = _mapper.Map<List<ChatMessageInfo>>(messages);

            var paginated = new PaginatedResult<ChatMessageInfo>
            {
                Items = items,
                Total = total,
                Page = page,
                MaxPage = maxPage
            };

            return Ok(Result<PaginatedResult<ChatMessageInfo>>.Success(paginated, "Mensagens listadas com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }
}
