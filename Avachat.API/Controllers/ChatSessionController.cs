using Microsoft.AspNetCore.Mvc;
using Avachat.Domain.DTOs;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.API.Controllers;

[ApiController]
public class ChatSessionController : ControllerBase
{
    private readonly IChatSessionRepository _sessionRepo;
    private readonly IChatMessageRepository _messageRepo;

    public ChatSessionController(IChatSessionRepository sessionRepo, IChatMessageRepository messageRepo)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
    }

    [HttpGet("api/agents/{agentId:long}/sessions")]
    public async Task<IActionResult> GetSessions(long agentId, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20)
    {
        try
        {
            tamanhoPagina = Math.Min(tamanhoPagina, 100);
            var sessions = await _sessionRepo.GetByAgentIdAsync(agentId, pagina, tamanhoPagina);
            var total = await _sessionRepo.CountByAgentIdAsync(agentId);

            var items = new List<ChatSessionInfo>();
            foreach (var s in sessions)
            {
                var msgCount = await _messageRepo.CountBySessionIdAsync(s.ChatSessionId);
                items.Add(new ChatSessionInfo
                {
                    ChatSessionId = s.ChatSessionId,
                    AgentId = s.AgentId,
                    UserName = s.UserName,
                    UserEmail = s.UserEmail,
                    UserPhone = s.UserPhone,
                    StartedAt = s.StartedAt,
                    EndedAt = s.EndedAt,
                    MessageCount = msgCount
                });
            }

            var paginated = new PaginatedResult<ChatSessionInfo>
            {
                Items = items,
                Total = total,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            };

            return Ok(Result<PaginatedResult<ChatSessionInfo>>.Success(paginated, "Sessoes listadas com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpGet("api/sessions/{sessionId:long}/messages")]
    public async Task<IActionResult> GetMessages(long sessionId, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 50)
    {
        try
        {
            tamanhoPagina = Math.Min(tamanhoPagina, 200);
            var messages = await _messageRepo.GetBySessionIdAsync(sessionId, pagina, tamanhoPagina);
            var total = await _messageRepo.CountBySessionIdAsync(sessionId);

            var items = messages.Select(m => new ChatMessageInfo
            {
                ChatMessageId = m.ChatMessageId,
                ChatSessionId = m.ChatSessionId,
                SenderType = (int)m.SenderType,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList();

            var paginated = new PaginatedResult<ChatMessageInfo>
            {
                Items = items,
                Total = total,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            };

            return Ok(Result<PaginatedResult<ChatMessageInfo>>.Success(paginated, "Mensagens listadas com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }
}
