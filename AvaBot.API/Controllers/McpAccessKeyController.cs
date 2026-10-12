using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.DTO;

namespace AvaBot.API.Controllers;

/// <summary>
/// Chaves de acesso para assistentes de IA (feature 017). Rotas do painel: JWT, nunca a chave MCP.
/// O valor em claro aparece so na resposta do POST e nunca e logado.
/// </summary>
[Authorize]
[ApiController]
[Route("auth/me/mcp-keys")]
public class McpAccessKeyController : ControllerBase
{
    public const string CreatedMessage = "Chave criada. Copie o valor agora: ele nao sera exibido de novo";

    private readonly McpAccessKeyService _keyService;
    private readonly IValidator<McpAccessKeyInsertInfo> _insertValidator;
    private readonly IMapper _mapper;

    public McpAccessKeyController(
        McpAccessKeyService keyService,
        IValidator<McpAccessKeyInsertInfo> insertValidator,
        IMapper mapper)
    {
        _keyService = keyService;
        _insertValidator = insertValidator;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        try
        {
            var keys = await _keyService.ListAsync(User.GetUserId());
            return Ok(Result<List<McpAccessKeyInfo>>.Success(_mapper.Map<List<McpAccessKeyInfo>>(keys), "Chaves listadas com sucesso"));
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
    public async Task<IActionResult> Create([FromBody] McpAccessKeyInsertInfo info)
    {
        try
        {
            var validation = await _insertValidator.ValidateAsync(info);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                return BadRequest(Result<object>.Failure(errors[0], errors));
            }

            var (key, plainKey) = await _keyService.CreateAsync(User.GetUserId(), info.Name);
            var created = new McpAccessKeyCreatedInfo
            {
                Key = plainKey,
                AccessKey = _mapper.Map<McpAccessKeyInfo>(key)
            };

            return Created($"/auth/me/mcp-keys/{key.McpAccessKeyId}", Result<McpAccessKeyCreatedInfo>.Success(created, CreatedMessage));
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
    public async Task<IActionResult> Revoke(long id)
    {
        try
        {
            var key = await _keyService.RevokeAsync(User.GetUserId(), id);
            return Ok(Result<McpAccessKeyInfo>.Success(_mapper.Map<McpAccessKeyInfo>(key), "Chave revogada"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure(McpAccessKeyService.NotFoundMessage));
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
