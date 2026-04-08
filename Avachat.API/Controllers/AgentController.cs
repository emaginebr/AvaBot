using Microsoft.AspNetCore.Mvc;
using Avachat.Domain.DTOs;
using Avachat.Domain.Models;
using Avachat.Application.Services;

namespace Avachat.API.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentController : ControllerBase
{
    private readonly AgentService _agentService;

    public AgentController(AgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var agents = await _agentService.GetAllAsync();
            var result = agents.Select(MapToInfo).ToList();
            return Ok(Result<List<AgentInfo>>.Success(result, "Agentes listados com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        try
        {
            var agent = await _agentService.GetBySlugAsync(slug);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(MapToInfo(agent)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpGet("{slug}/chat-config")]
    public async Task<IActionResult> GetChatConfig(string slug)
    {
        try
        {
            var agent = await _agentService.GetBySlugAsync(slug);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            if (agent.Status == 0)
                return Ok(Result<object>.Failure("Agente temporariamente indisponivel"));

            var config = new AgentChatConfigInfo
            {
                Name = agent.Name,
                Description = agent.Description,
                CollectName = agent.CollectName,
                CollectEmail = agent.CollectEmail,
                CollectPhone = agent.CollectPhone
            };

            return Ok(Result<AgentChatConfigInfo>.Success(config));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgentInsertInfo info)
    {
        try
        {
            if (await _agentService.SlugExistsAsync(info.Slug))
                return BadRequest(Result<object>.Failure("Slug ja esta em uso", new[] { "slug" }));

            var agent = await _agentService.CreateAsync(info);
            return Created($"/api/agents/{agent.Slug}", Result<AgentInfo>.Success(MapToInfo(agent), "Agente criado com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] AgentInsertInfo info)
    {
        try
        {
            if (await _agentService.SlugExistsAsync(info.Slug, id))
                return BadRequest(Result<object>.Failure("Slug ja esta em uso", new[] { "slug" }));

            var agent = await _agentService.UpdateAsync(id, info);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(MapToInfo(agent), "Agente atualizado com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var deleted = await _agentService.DeleteAsync(id);
            if (!deleted)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<object>.Success(null!, "Agente removido com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> ToggleStatus(long id)
    {
        try
        {
            var agent = await _agentService.ToggleStatusAsync(id);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(MapToInfo(agent), "Status atualizado com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    private static AgentInfo MapToInfo(Agent agent) => new()
    {
        AgentId = agent.AgentId,
        Name = agent.Name,
        Slug = agent.Slug,
        Description = agent.Description,
        SystemPrompt = agent.SystemPrompt,
        Status = agent.Status,
        CollectName = agent.CollectName,
        CollectEmail = agent.CollectEmail,
        CollectPhone = agent.CollectPhone,
        CreatedAt = agent.CreatedAt,
        UpdatedAt = agent.UpdatedAt
    };
}
