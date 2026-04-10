using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Avachat.Application.Services;
using Avachat.DTO;
using Telegram.Bot.Types;

namespace Avachat.API.Controllers;

[ApiController]
public class TelegramController : ControllerBase
{
    private readonly TelegramService _telegramService;
    private readonly IConfiguration _configuration;

    public TelegramController(TelegramService telegramService, IConfiguration configuration)
    {
        _telegramService = telegramService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("telegram/webhook")]
    public async Task<IActionResult> Webhook([FromBody] Update update)
    {
        var expectedSecret = _configuration["Telegram:WebhookSecret"];
        var receivedSecret = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedSecret) || receivedSecret != expectedSecret)
            return Unauthorized();

        try
        {
            await _telegramService.ProcessUpdateAsync(update);
            return Ok();
        }
        catch (Exception)
        {
            // Always return 200 to Telegram to avoid retries
            return Ok();
        }
    }

    [Authorize]
    [HttpPost("telegram/setup-webhook")]
    public async Task<IActionResult> SetupWebhook()
    {
        try
        {
            await _telegramService.SetupWebhookAsync();

            return Ok(Result<object>.Success(new
            {
                url = _configuration["Telegram:WebhookUrl"],
                status = "registered"
            }, "Webhook registrado com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure($"Erro ao registrar webhook: {ex.Message}"));
        }
    }
}
