using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.Application.Services;
using AvaBot.DTO;
using Telegram.Bot.Types;

namespace AvaBot.API.Controllers;

[ApiController]
public class TelegramController : ControllerBase
{
    private readonly TelegramService _telegramService;
    private readonly AgentService _agentService;

    public TelegramController(TelegramService telegramService, AgentService agentService)
    {
        _telegramService = telegramService;
        _agentService = agentService;
    }

    [AllowAnonymous]
    [HttpPost("telegram/{slug}/webhook")]
    public async Task<IActionResult> Webhook(string slug, [FromBody] Update update)
    {
        var agent = await _agentService.GetBySlugAsync(slug);
        if (agent == null || agent.Status == 0 || string.IsNullOrEmpty(agent.TelegramBotToken))
            return Unauthorized();

        var receivedSecret = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(agent.TelegramWebhookSecret) || receivedSecret != agent.TelegramWebhookSecret)
            return Unauthorized();

        try
        {
            await _telegramService.ProcessUpdateAsync(agent, update);
            return Ok();
        }
        catch (Exception)
        {
            return Ok();
        }
    }

    [Authorize]
    [HttpPost("telegram/{id:long}/setup-webhook")]
    public async Task<IActionResult> SetupWebhook(long id)
    {
        try
        {
            var result = await _telegramService.SetupWebhookAsync(id);
            return Ok(Result<TelegramWebhookInfo>.Success(result, "Webhook registrado com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure($"Erro ao registrar webhook: {ex.Message}"));
        }
    }

    [Authorize]
    [HttpGet("telegram/{id:long}/webhook-info")]
    public async Task<IActionResult> GetWebhookInfo(long id)
    {
        try
        {
            var result = await _telegramService.GetWebhookInfoAsync(id);
            return Ok(Result<TelegramWebhookInfo>.Success(result, "Informacao do webhook obtida com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure($"Erro ao consultar webhook: {ex.Message}"));
        }
    }

    [Authorize]
    [HttpPost("telegram/{id:long}/regenerate-secret")]
    public async Task<IActionResult> RegenerateSecret(long id)
    {
        try
        {
            var result = await _telegramService.RegenerateWebhookSecretAsync(id);
            return Ok(Result<TelegramWebhookInfo>.Success(result, "Secret regenerado e webhook atualizado com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure($"Erro ao regenerar secret: {ex.Message}"));
        }
    }
}
