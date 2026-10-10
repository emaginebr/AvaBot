using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "Credenciais invalidas";

    private readonly UserService _userService;
    private readonly JwtTokenIssuer _tokenIssuer;
    private readonly IMapper _mapper;
    private readonly IValidator<UserRegisterInfo> _registerValidator;
    private readonly IValidator<UserLoginInfo> _loginValidator;
    private readonly IValidator<UserUpdateInfo> _updateValidator;
    private readonly IValidator<UserPasswordChangeInfo> _passwordValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserService userService,
        JwtTokenIssuer tokenIssuer,
        IMapper mapper,
        IValidator<UserRegisterInfo> registerValidator,
        IValidator<UserLoginInfo> loginValidator,
        IValidator<UserUpdateInfo> updateValidator,
        IValidator<UserPasswordChangeInfo> passwordValidator,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _tokenIssuer = tokenIssuer;
        _mapper = mapper;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _updateValidator = updateValidator;
        _passwordValidator = passwordValidator;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterInfo info)
    {
        var validation = await _registerValidator.ValidateAsync(info);
        if (!validation.IsValid)
            return BadRequest(ValidationFailure<AuthResultInfo>(validation));

        try
        {
            var user = await _userService.RegisterAsync(info);
            var result = BuildAuthResult(user);
            return Created("/auth/me", Result<AuthResultInfo>.Success(result, "Conta criada com sucesso"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Result<AuthResultInfo>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar conta");
            return StatusCode(500, Result<AuthResultInfo>.Failure("Erro ao criar conta"));
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginInfo info)
    {
        var validation = await _loginValidator.ValidateAsync(info);
        if (!validation.IsValid)
            return BadRequest(ValidationFailure<AuthResultInfo>(validation));

        try
        {
            var user = await _userService.AuthenticateAsync(info.Email, info.Password);
            if (user == null)
                return Unauthorized(Result<AuthResultInfo>.Failure(InvalidCredentialsMessage));

            var result = BuildAuthResult(user);
            return Ok(Result<AuthResultInfo>.Success(result, "Login realizado com sucesso"));
        }
        catch (InvalidOperationException ex)
        {
            // Bloqueio por excesso de tentativas (FR-007): mesma familia de mensagem generica.
            return Unauthorized(Result<AuthResultInfo>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar");
            return StatusCode(500, Result<AuthResultInfo>.Failure("Erro ao autenticar"));
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        try
        {
            var user = await _userService.GetAsync(User.GetUserId());
            if (user == null)
                return NotFound(Result<UserInfo>.Failure("Usuario nao encontrado"));

            return Ok(Result<UserInfo>.Success(_mapper.Map<UserInfo>(user)));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<UserInfo>.Failure(InvalidCredentialsMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar conta");
            return StatusCode(500, Result<UserInfo>.Failure("Erro ao consultar conta"));
        }
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UserUpdateInfo info)
    {
        var validation = await _updateValidator.ValidateAsync(info);
        if (!validation.IsValid)
            return BadRequest(ValidationFailure<UserInfo>(validation));

        try
        {
            var user = await _userService.UpdateNameAsync(User.GetUserId(), info.Name);
            return Ok(Result<UserInfo>.Success(_mapper.Map<UserInfo>(user), "Conta atualizada com sucesso"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<UserInfo>.Failure(InvalidCredentialsMessage));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<UserInfo>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar conta");
            return StatusCode(500, Result<UserInfo>.Failure("Erro ao atualizar conta"));
        }
    }

    [Authorize]
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] UserPasswordChangeInfo info)
    {
        var validation = await _passwordValidator.ValidateAsync(info);
        if (!validation.IsValid)
            return BadRequest(ValidationFailure<object>(validation));

        try
        {
            await _userService.ChangePasswordAsync(User.GetUserId(), info.CurrentPassword, info.NewPassword);
            return Ok(Result<object>.Success(null!, "Senha alterada com sucesso"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure(InvalidCredentialsMessage));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar senha");
            return StatusCode(500, Result<object>.Failure("Erro ao alterar senha"));
        }
    }

    private AuthResultInfo BuildAuthResult(User user)
    {
        var (token, expiresAt) = _tokenIssuer.Issue(user);
        return new AuthResultInfo
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = _mapper.Map<UserInfo>(user)
        };
    }

    private static Result<T> ValidationFailure<T>(FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
        return Result<T>.Failure(errors.FirstOrDefault() ?? "Dados invalidos", errors);
    }
}
