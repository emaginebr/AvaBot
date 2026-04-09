using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Avachat.API.Controllers;

namespace Avachat.Tests.API.Controllers;

public class AuthControllerTest
{
    private readonly AuthController _sut;

    public AuthControllerTest()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Username"] = "admin",
                ["Auth:Password"] = "admin@123",
                ["Auth:JwtSecret"] = "avachat-test-secret-key-minimum-32-chars!",
                ["Auth:TokenExpirationMinutes"] = "60"
            })
            .Build();

        _sut = new AuthController(config);
    }

    [Fact]
    public void Login_ShouldReturnOk_WithToken_WhenCredentialsAreValid()
    {
        var result = _sut.Login(new LoginRequest("admin", "admin@123"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value!;
        var sucesso = value.GetType().GetProperty("sucesso")!.GetValue(value);
        var token = value.GetType().GetProperty("token")!.GetValue(value) as string;

        Assert.True((bool)sucesso!);
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void Login_ShouldReturnUnauthorized_WhenUsernameIsWrong()
    {
        var result = _sut.Login(new LoginRequest("wrong", "admin@123"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        var result = _sut.Login(new LoginRequest("admin", "wrong"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
