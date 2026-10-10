using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Auth;
using AvaBot.API.Controllers;
using AvaBot.API.Validators;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Controllers;

public class AuthControllerTest
{
    private const string Secret = "avabot-test-secret-key-minimum-32-chars!";

    private readonly Mock<IUserRepository<User>> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly AuthController _sut;

    public AuthControllerTest()
    {
        // Hasher deterministico: "hash:<senha>" (o hasher real e testado em Pbkdf2PasswordHasherTest).
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hash:{p}");
        _hasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string p, string stored) => stored == $"hash:{p}");

        _sut = CreateController(BuildConfig("43200"));
    }

    private AuthController CreateController(IConfiguration config)
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<UserProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var userService = new UserService(_userRepoMock.Object, _hasherMock.Object, _cache, NullLogger<UserService>.Instance);

        return new AuthController(
            userService,
            new JwtTokenIssuer(config),
            mapper,
            new UserRegisterInfoValidator(),
            new UserLoginInfoValidator(),
            new UserUpdateInfoValidator(),
            new UserPasswordChangeInfoValidator(),
            NullLogger<AuthController>.Instance);
    }

    private static IConfiguration BuildConfig(string? expirationMinutes)
    {
        var values = new Dictionary<string, string?> { ["Auth:JwtSecret"] = Secret };
        if (expirationMinutes != null)
            values["Auth:TokenExpirationMinutes"] = expirationMinutes;

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static User ExistingUser(long id = 7, string email = "ana@exemplo.com", int status = 1) => new()
    {
        UserId = id,
        Name = "Ana",
        Email = email,
        PasswordHash = "hash:senha-certa-123",
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    private void ArrangeUser(User? user, string email = "ana@exemplo.com")
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_ShouldReturnCreated_WithTokenAndNormalizedEmail()
    {
        _userRepoMock.Setup(r => r.EmailExistsAsync("ana@exemplo.com")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.UserId = 7; return u; });

        var result = await _sut.Register(new UserRegisterInfo { Name = " Ana Souza ", Email = " Ana@Exemplo.com ", Password = "minhasenha123" });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/auth/me", created.Location);
        var response = Assert.IsType<Result<AuthResultInfo>>(created.Value);
        Assert.True(response.Sucesso);
        Assert.False(string.IsNullOrEmpty(response.Dados!.Token));
        Assert.Equal("ana@exemplo.com", response.Dados.User.Email);
        Assert.Equal("Ana Souza", response.Dados.User.Name);
        Assert.Equal(7, response.Dados.User.UserId);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailIsInUse()
    {
        _userRepoMock.Setup(r => r.EmailExistsAsync("ana@exemplo.com")).ReturnsAsync(true);

        var result = await _sut.Register(new UserRegisterInfo { Name = "Ana", Email = "ANA@exemplo.com", Password = "minhasenha123" });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<Result<AuthResultInfo>>(conflict.Value);
        Assert.Equal("Este e-mail ja esta em uso", response.Mensagem);
        _userRepoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsTooShort()
    {
        var result = await _sut.Register(new UserRegisterInfo { Name = "Ana", Email = "ana@exemplo.com", Password = "curta" });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<Result<AuthResultInfo>>(bad.Value);
        Assert.Contains("8 caracteres", response.Mensagem);
        _userRepoMock.Verify(r => r.EmailExistsAsync(It.IsAny<string>()), Times.Never);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_ShouldReturnOk_WithTokenCarryingTheUserId()
    {
        ArrangeUser(ExistingUser());

        var result = await _sut.Login(new UserLoginInfo { Email = "Ana@Exemplo.com", Password = "senha-certa-123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<AuthResultInfo>>(ok.Value);
        Assert.True(response.Sucesso);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Dados!.Token);
        Assert.Equal("7", jwt.Subject);
        Assert.Equal("ana@exemplo.com", jwt.Claims.Single(c => c.Type == "email").Value);
        Assert.Equal("Ana", jwt.Claims.Single(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WithGenericMessage_WhenPasswordIsWrong()
    {
        ArrangeUser(ExistingUser());

        var result = await _sut.Login(new UserLoginInfo { Email = "ana@exemplo.com", Password = "errada" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<Result<AuthResultInfo>>(unauthorized.Value);
        Assert.Equal("Credenciais invalidas", response.Mensagem);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WithTheSameMessage_WhenEmailDoesNotExist()
    {
        ArrangeUser(null, "ninguem@exemplo.com");

        var result = await _sut.Login(new UserLoginInfo { Email = "ninguem@exemplo.com", Password = "qualquer-coisa" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Credenciais invalidas", Assert.IsType<Result<AuthResultInfo>>(unauthorized.Value).Mensagem);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WithLockMessage_AfterFiveFailures()
    {
        ArrangeUser(ExistingUser());

        for (var i = 0; i < 5; i++)
            await _sut.Login(new UserLoginInfo { Email = "ana@exemplo.com", Password = "errada" });

        var result = await _sut.Login(new UserLoginInfo { Email = "ana@exemplo.com", Password = "senha-certa-123" });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<Result<AuthResultInfo>>(unauthorized.Value);
        Assert.Contains("15 minutos", response.Mensagem);
    }

    // ---------- Validade do token (US3) ----------

    [Fact]
    public async Task Login_ShouldIssueATokenValidForThirtyDays()
    {
        ArrangeUser(ExistingUser());

        var result = await _sut.Login(new UserLoginInfo { Email = "ana@exemplo.com", Password = "senha-certa-123" });

        var response = Assert.IsType<Result<AuthResultInfo>>(Assert.IsType<OkObjectResult>(result).Value);
        var days = (response.Dados!.ExpiresAt - DateTime.UtcNow).TotalDays;
        Assert.InRange(days, 29.9, 30.1);
    }

    [Fact]
    public async Task Login_ShouldDefaultToThirtyDays_WhenExpirationIsNotConfigured()
    {
        ArrangeUser(ExistingUser());
        var sut = CreateController(BuildConfig(null));

        var result = await sut.Login(new UserLoginInfo { Email = "ana@exemplo.com", Password = "senha-certa-123" });

        var response = Assert.IsType<Result<AuthResultInfo>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.InRange((response.Dados!.ExpiresAt - DateTime.UtcNow).TotalDays, 29.9, 30.1);
    }

    // ---------- Conta (US5) ----------

    [Fact]
    public async Task Me_ShouldReturnTheUser_WithoutThePasswordHash()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(ExistingUser());

        var result = await _sut.WithUser(7).Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<UserInfo>>(ok.Value);
        Assert.Equal("ana@exemplo.com", response.Dados!.Email);
        Assert.DoesNotContain("hash", System.Text.Json.JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task Me_ShouldReturnUnauthorized_WhenTheTokenHasNoUserId()
    {
        var result = await _sut.WithoutUser().Me();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task UpdateMe_ShouldReturnOk_WithTheNewName()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(ExistingUser());
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.WithUser(7).UpdateMe(new UserUpdateInfo { Name = "  Ana S.  " });

        var response = Assert.IsType<Result<UserInfo>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Ana S.", response.Dados!.Name);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenCurrentPasswordIsWrong()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(ExistingUser());

        var result = await _sut.WithUser(7).ChangePassword(new UserPasswordChangeInfo { CurrentPassword = "errada", NewPassword = "nova-senha-123" });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Senha atual incorreta", Assert.IsType<Result<object>>(bad.Value).Mensagem);
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_WhenCurrentPasswordIsRight()
    {
        var user = ExistingUser();
        _userRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _sut.WithUser(7).ChangePassword(new UserPasswordChangeInfo { CurrentPassword = "senha-certa-123", NewPassword = "nova-senha-123" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("hash:nova-senha-123", user.PasswordHash);
    }
}
