using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.DTO;

namespace AvaBot.API.Controllers;

[ApiController]
public class WhatsappController : ControllerBase
{
    private readonly WhatsappService _whatsappService;

    public WhatsappController(WhatsappService whatsappService)
    {
        _whatsappService = whatsappService;
    }

    [Authorize]
    [HttpPost("whatsapp/{slug}/start-session")]
    public async Task<IActionResult> StartSession(string slug)
    {
        try
        {
            var result = await _whatsappService.StartSessionAsync(slug, User.GetUserId());
            return Ok(Result<WhatsappStatusInfo>.Success(result, "Sessao iniciada com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
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
            return StatusCode(500, Result<object>.Failure($"Erro ao iniciar sessao: {ex.Message}"));
        }
    }

    [Authorize]
    [HttpGet("whatsapp/{slug}/qrcode")]
    public async Task<IActionResult> GetQrCode(string slug)
    {
        try
        {
            var result = await _whatsappService.GetQrCodeAsync(slug, User.GetUserId());
            return Ok(Result<WhatsappQrCodeInfo>.Success(result, "QR code obtido com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
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
            return StatusCode(500, Result<object>.Failure($"Erro ao obter QR code: {ex.Message}"));
        }
    }

    [Authorize]
    [HttpGet("whatsapp/{slug}/status")]
    public async Task<IActionResult> GetStatus(string slug)
    {
        try
        {
            var result = await _whatsappService.GetStatusAsync(slug, User.GetUserId());
            return Ok(Result<WhatsappStatusInfo>.Success(result, "Status obtido com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
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
            return StatusCode(500, Result<object>.Failure($"Erro ao consultar status: {ex.Message}"));
        }
    }

    [Authorize]
    [HttpPost("whatsapp/{slug}/disconnect")]
    public async Task<IActionResult> Disconnect(string slug)
    {
        try
        {
            var result = await _whatsappService.DisconnectAsync(slug, User.GetUserId());
            return Ok(Result<WhatsappStatusInfo>.Success(result, "Sessao encerrada com sucesso"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(Result<object>.Failure("Agente nao encontrado"));
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
            return StatusCode(500, Result<object>.Failure($"Erro ao encerrar sessao: {ex.Message}"));
        }
    }

    [AllowAnonymous]
    [HttpPost("whatsapp/{slug}/webhook")]
    public async Task<IActionResult> Webhook(string slug, [FromBody] JsonElement payload)
    {
        try
        {
            await _whatsappService.ProcessWebhookAsync(slug, payload);
            return Ok();
        }
        catch (Exception)
        {
            return Ok();
        }
    }
}
