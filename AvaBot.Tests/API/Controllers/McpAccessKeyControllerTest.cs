using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Controllers;
using AvaBot.API.Validators;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Controllers;

public class McpAccessKeyControllerTest
{
    private const long OwnerId = 7;
    private readonly Mock<IMcpAccessKeyRepository<McpAccessKey>> _repoMock = new();
    private readonly McpAccessKeyController _sut;

    public McpAccessKeyControllerTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<McpAccessKeyProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<McpAccessKey>())).ReturnsAsync((McpAccessKey k) => { k.McpAccessKeyId = 3; k.CreatedAt = DateTime.UtcNow; return k; });
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<McpAccessKey>())).ReturnsAsync((McpAccessKey k) => k);

        var service = new McpAccessKeyService(_repoMock.Object, NullLogger<McpAccessKeyService>.Instance);
        _sut = new McpAccessKeyController(service, new McpAccessKeyInsertInfoValidator(), mapper).WithUser(OwnerId);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WithThePlainKeyAndThePrefix()
    {
        _repoMock.Setup(r => r.CountActiveByUserAsync(OwnerId)).ReturnsAsync(0);

        var result = await _sut.Create(new McpAccessKeyInsertInfo { Name = "Claude do notebook" });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/auth/me/mcp-keys/3", created.Location);
        var body = Assert.IsType<Result<McpAccessKeyCreatedInfo>>(created.Value);
        Assert.True(body.Sucesso);
        Assert.Equal("Chave criada. Copie o valor agora: ele nao sera exibido de novo", body.Mensagem);
        Assert.StartsWith("avabot_mcp_", body.Dados!.Key);
        Assert.Equal(54, body.Dados.Key.Length);
        Assert.Equal(body.Dados.Key.Substring(11, 8), body.Dados.AccessKey.KeyPrefix);
        Assert.Equal("Claude do notebook", body.Dados.AccessKey.Name);
        Assert.Null(body.Dados.AccessKey.LastUsedAt);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var result = await _sut.Create(new McpAccessKeyInsertInfo { Name = "   " });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<Result<object>>(bad.Value);
        Assert.Equal("Nome e obrigatorio", body.Mensagem);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<McpAccessKey>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenTheActiveKeyLimitIsReached()
    {
        _repoMock.Setup(r => r.CountActiveByUserAsync(OwnerId)).ReturnsAsync(20);

        var result = await _sut.Create(new McpAccessKeyInsertInfo { Name = "Mais uma" });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<Result<object>>(bad.Value);
        Assert.Equal(McpAccessKeyService.LimitMessage, body.Mensagem);
    }

    [Fact]
    public async Task Revoke_ShouldReturnNotFound_WhenTheKeyDoesNotBelongToTheUser()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((McpAccessKey?)null);

        var result = await _sut.Revoke(99);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var body = Assert.IsType<Result<object>>(notFound.Value);
        Assert.Equal("Chave nao encontrada", body.Mensagem);
    }

    [Fact]
    public async Task Revoke_ShouldReturnOk_WithStatusZero()
    {
        _repoMock.Setup(r => r.GetByIdAsync(3, OwnerId)).ReturnsAsync(new McpAccessKey { McpAccessKeyId = 3, UserId = OwnerId, Name = "Claude", Status = 1 });

        var result = await _sut.Revoke(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<Result<McpAccessKeyInfo>>(ok.Value);
        Assert.Equal("Chave revogada", body.Mensagem);
        Assert.Equal(0, body.Dados!.Status);
        Assert.NotNull(body.Dados.RevokedAt);
    }

    [Fact]
    public async Task List_ShouldReturnTheKeys_WithoutTheHash()
    {
        _repoMock.Setup(r => r.GetAllByUserAsync(OwnerId)).ReturnsAsync(
        [
            new McpAccessKey { McpAccessKeyId = 3, UserId = OwnerId, Name = "Claude", KeyHash = "abc", KeyPrefix = "Ab3kQ9xZ", Status = 1 }
        ]);

        var result = await _sut.List();

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<Result<List<McpAccessKeyInfo>>>(ok.Value);
        var item = Assert.Single(body.Dados!);
        Assert.Equal("Ab3kQ9xZ", item.KeyPrefix);
        Assert.Null(typeof(McpAccessKeyInfo).GetProperty("KeyHash"));
    }

    [Fact]
    public async Task List_ShouldReturnUnauthorized_WhenTheTokenHasNoUser()
    {
        var result = await _sut.WithoutUser().List();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
