using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.DTO;
using AvaBot.Domain.Models;
using AvaBot.Application.Services;
using AvaBot.API.Auth;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.API.Controllers;

[Authorize]
[ApiController]
[Route("agents")]
public class AgentController : ControllerBase
{
    private readonly AgentService _agentService;
    private readonly SearchService _searchService;
    private readonly ChatService _chatService;
    private readonly IOpenAIService _openAIService;
    private readonly IMapper _mapper;

    public AgentController(
        AgentService agentService,
        SearchService searchService,
        ChatService chatService,
        IOpenAIService openAIService,
        IMapper mapper)
    {
        _agentService = agentService;
        _searchService = searchService;
        _chatService = chatService;
        _openAIService = openAIService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var agents = await _agentService.GetAllAsync(User.GetUserId());
            var result = _mapper.Map<List<AgentInfo>>(agents);
            return Ok(Result<List<AgentInfo>>.Success(result, "Agentes listados com sucesso"));
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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
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
            var agent = await _agentService.CreateAsync(info, User.GetUserId());
            return Created($"/agents/{agent.Slug}", Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent), "Agente criado com sucesso"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
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

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] AgentInsertInfo info)
    {
        try
        {
            var agent = await _agentService.UpdateAsync(id, info, User.GetUserId());
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent), "Agente atualizado com sucesso"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var deleted = await _agentService.DeleteAsync(id, User.GetUserId());
            if (!deleted)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<object>.Success(null!, "Agente removido com sucesso"));
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

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> ToggleStatus(long id)
    {
        try
        {
            var agent = await _agentService.ToggleStatusAsync(id, User.GetUserId());
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            return Ok(Result<AgentInfo>.Success(_mapper.Map<AgentInfo>(agent), "Status atualizado com sucesso"));
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

    [HttpGet("{id:long}/search")]
    public async Task<IActionResult> Search(long id, [FromQuery] string query, [FromQuery] int topK = 5)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(Result<object>.Failure("O parametro 'query' e obrigatorio"));

            if (await _agentService.GetOwnedByIdAsync(id, User.GetUserId()) == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            var chunks = await _searchService.SearchAsync(id, query, topK);
            return Ok(Result<List<string>>.Success(chunks, $"{chunks.Count} resultado(s) encontrado(s)"));
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

    [HttpPost("{id:long}/test")]
    public async Task<IActionResult> TestQuestion(long id, [FromBody] AgentTestQuestionInfo info)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(info.Query))
                return BadRequest(Result<object>.Failure("O parametro 'query' e obrigatorio"));

            var agent = await _agentService.GetOwnedByIdAsync(id, User.GetUserId());
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            if (info.History != null)
            {
                for (var i = 0; i < info.History.Count; i++)
                {
                    var item = info.History[i];

                    if (item.Role is not ("user" or "assistant"))
                        return BadRequest(Result<object>.Failure($"Historico invalido: item {i + 1} tem role '{item.Role}' (use 'user' ou 'assistant')"));

                    if (string.IsNullOrWhiteSpace(item.Content))
                        return BadRequest(Result<object>.Failure($"Historico invalido: item {i + 1} esta vazio"));
                }
            }

            var result = await _chatService.TestMessageAsync(id, agent.ChatModel, agent.SystemPrompt, info.Query, info.History);
            return Ok(Result<AgentTestResultInfo>.Success(result));
        }
        catch (AgentTestFailedException ex)
        {
            // O painel le so a mensagem; o relatorio de calibracao usa as rodadas concluidas em dados.
            return StatusCode(500, new Result<AgentTestResultInfo>
            {
                Sucesso = false,
                Mensagem = ex.Message,
                Dados = ex.PartialResult
            });
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

    [HttpPost("{id:long}/openai/diagnose")]
    public async Task<IActionResult> DiagnoseOpenAI(long id, [FromBody] AgentOpenAIDiagnoseInfo? info)
    {
        try
        {
            var agent = await _agentService.GetOwnedByIdAsync(id, User.GetUserId());
            if (agent == null)
                return NotFound(Result<object>.Failure("Agente nao encontrado"));

            // Sem chave no corpo, usa a credencial salva (e falha com orientacao se nao houver).
            var apiKey = !string.IsNullOrWhiteSpace(info?.ApiKey)
                ? info!.ApiKey!.Trim()
                : await _agentService.GetOpenAIApiKeyAsync(id, User.GetUserId());

            var check = await _openAIService.TestApiKeyAsync(apiKey);

            return Ok(Result<AgentOpenAIDiagnoseResultInfo>.Success(
                new AgentOpenAIDiagnoseResultInfo { Success = check.Success, Message = check.Message },
                "Diagnóstico concluído"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
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
