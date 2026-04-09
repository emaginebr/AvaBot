using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Avachat.DTO;
using Avachat.Domain.Models;
using Avachat.Application.Services;

namespace Avachat.API.Controllers;

[Authorize]
[ApiController]
[Route("agents")]
public class AgentController : ControllerBase
{
    private readonly AgentService _agentService;
    private readonly SearchService _searchService;
    private readonly IMapper _mapper;

    public AgentController(AgentService agentService, SearchService searchService, IMapper mapper)
    {
        _agentService = agentService;
        _searchService = searchService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var agents = await _agentService.GetAllAsync();
            var result = _mapper.Map<List<AgentInfo>>(agents);
            return Ok(Result<List<AgentInfo>>.Success(result, "Agentes listados com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [AllowAnonymous]
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        try
        {
            var agent = await _agentService.GetBySlugAsync(slug);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [AllowAnonymous]
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

            var config = _mapper.Map<AgentChatConfigInfo>(agent);
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
            var agent = await _agentService.CreateAsync(info);
            return Created($"/agents/{agent.Slug}", Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent), "Agente criado com sucesso"));
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
            var agent = await _agentService.UpdateAsync(id, info);
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent), "Agente atualizado com sucesso"));
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

            return Ok(Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent), "Status atualizado com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpGet("{id:long}/search")]
    public async Task<IActionResult> Search(long id, [FromQuery] string query, [FromQuery] int topK = 5)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(Result<object>.Failure("O parametro 'query' e obrigatorio"));

            var chunks = await _searchService.SearchAsync(id, query, topK);
            return Ok(Result<List<string>>.Success(chunks, $"{chunks.Count} resultado(s) encontrado(s)"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }
}
