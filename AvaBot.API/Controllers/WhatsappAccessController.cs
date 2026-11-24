using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Controllers;

/// <summary>
/// Controle de acesso do WhatsApp (feature 018): modo, frases prontas, contatos autorizados e pedidos de acesso.
/// Nao exige sessao do WhatsApp iniciada: o dono pode montar a lista antes de parear.
/// </summary>
[Authorize]
[ApiController]
public class WhatsappAccessController : ControllerBase
{
    public const string AgentNotFoundMessage = "Agente nao encontrado";

    private readonly AgentService _agentService;
    private readonly WhatsappAccessService _accessService;
    private readonly IValidator<WhatsappAccessConfigUpdateInfo> _configValidator;
    private readonly IValidator<WhatsappContactInsertInfo> _contactValidator;
    private readonly IValidator<WhatsappAccessRequestApproveInfo> _approveValidator;

    public WhatsappAccessController(
        AgentService agentService,
        WhatsappAccessService accessService,
        IValidator<WhatsappAccessConfigUpdateInfo> configValidator,
        IValidator<WhatsappContactInsertInfo> contactValidator,
        IValidator<WhatsappAccessRequestApproveInfo> approveValidator)
    {
        _agentService = agentService;
        _accessService = accessService;
        _configValidator = configValidator;
        _contactValidator = contactValidator;
        _approveValidator = approveValidator;
    }

    // ------------------------------------------------------------------ Configuracao

    [HttpGet("whatsapp/{slug}/access")]
    public Task<IActionResult> GetConfig(string slug) => ExecuteAsync("obter configuracao", async () =>
    {
        var agent = await ResolveAgentAsync(slug);
        return Ok(Result<WhatsappAccessConfigInfo>.Success(await _accessService.GetConfigAsync(agent), "Configuracao obtida com sucesso"));
    });

    [HttpPut("whatsapp/{slug}/access")]
    public Task<IActionResult> UpdateConfig(string slug, [FromBody] WhatsappAccessConfigUpdateInfo info) => ExecuteAsync("salvar configuracao", async () =>
    {
        await ValidateAsync(_configValidator, info);
        var agent = await ResolveAgentAsync(slug);
        return Ok(Result<WhatsappAccessConfigInfo>.Success(await _accessService.UpdateConfigAsync(agent, info), "Configuracao salva com sucesso"));
    });

    // ------------------------------------------------------------------ Contatos

    [HttpGet("whatsapp/{slug}/contacts")]
    public Task<IActionResult> ListContacts(string slug) => ExecuteAsync("listar contatos", async () =>
    {
        var agent = await ResolveAgentAsync(slug);
        return Ok(Result<List<WhatsappContactInfo>>.Success(await _accessService.ListContactsAsync(agent), "Contatos listados com sucesso"));
    });

    [HttpPost("whatsapp/{slug}/contacts")]
    public Task<IActionResult> AddContact(string slug, [FromBody] WhatsappContactInsertInfo info) => ExecuteAsync("adicionar contato", async () =>
    {
        await ValidateAsync(_contactValidator, info);
        var agent = await ResolveAgentAsync(slug);
        return Ok(Result<WhatsappContactInfo>.Success(await _accessService.AddContactAsync(agent, info), "Contato adicionado com sucesso"));
    });

    [HttpPut("whatsapp/{slug}/contacts/{contactId:long}")]
    public Task<IActionResult> UpdateContact(string slug, long contactId, [FromBody] WhatsappContactInsertInfo info) => ExecuteAsync("atualizar contato", async () =>
    {
        await ValidateAsync(_contactValidator, info);
        var agent = await ResolveAgentAsync(slug);
        return Ok(Result<WhatsappContactInfo>.Success(await _accessService.UpdateContactAsync(agent, contactId, info), "Contato atualizado com sucesso"));
    });

    [HttpDelete("whatsapp/{slug}/contacts/{contactId:long}")]
    public Task<IActionResult> RemoveContact(string slug, long contactId) => ExecuteAsync("remover contato", async () =>
    {
        var agent = await ResolveAgentAsync(slug);
        await _accessService.RemoveContactAsync(agent, contactId);
        return Ok(Result<bool>.Success(true, "Contato removido com sucesso"));
    });

    // ------------------------------------------------------------------ Pedidos de acesso

    [HttpGet("whatsapp/{slug}/access-requests")]
    public Task<IActionResult> ListRequests(string slug, [FromQuery] int? status) => ExecuteAsync("listar solicitacoes", async () =>
    {
        var agent = await ResolveAgentAsync(slug);
        return Ok(Result<List<WhatsappAccessRequestInfo>>.Success(await _accessService.ListRequestsAsync(agent, status), "Solicitacoes listadas com sucesso"));
    });

    [HttpPost("whatsapp/{slug}/access-requests/{requestId:long}/approve")]
    public Task<IActionResult> ApproveRequest(string slug, long requestId, [FromBody] WhatsappAccessRequestApproveInfo? info) => ExecuteAsync("aprovar solicitacao", async () =>
    {
        info ??= new WhatsappAccessRequestApproveInfo();
        await ValidateAsync(_approveValidator, info);
        var agent = await ResolveAgentAsync(slug);
        var contact = await _accessService.ApproveRequestAsync(agent, requestId, info.Name, User.GetUserId());
        return Ok(Result<WhatsappContactInfo>.Success(contact, "Solicitacao aprovada com sucesso"));
    });

    [HttpPost("whatsapp/{slug}/access-requests/{requestId:long}/reject")]
    public Task<IActionResult> RejectRequest(string slug, long requestId) => ExecuteAsync("rejeitar solicitacao", async () =>
    {
        var agent = await ResolveAgentAsync(slug);
        var request = await _accessService.RejectRequestAsync(agent, requestId, User.GetUserId());
        return Ok(Result<WhatsappAccessRequestInfo>.Success(request, "Solicitacao rejeitada com sucesso"));
    });

    // ------------------------------------------------------------------ Apoio

    private async Task<Agent> ResolveAgentAsync(string slug) =>
        await _agentService.GetOwnedBySlugAsync(slug, User.GetUserId())
            ?? throw new KeyNotFoundException(AgentNotFoundMessage);

    private static async Task ValidateAsync<T>(IValidator<T> validator, T info)
    {
        var validation = await validator.ValidateAsync(info);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);
    }

    private async Task<IActionResult> ExecuteAsync(string action, Func<Task<IActionResult>> body)
    {
        try
        {
            return await body();
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToArray();
            return BadRequest(Result<object>.Failure(errors.FirstOrDefault() ?? ex.Message, errors));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure($"Erro ao {action}: {ex.Message}"));
        }
    }
}
