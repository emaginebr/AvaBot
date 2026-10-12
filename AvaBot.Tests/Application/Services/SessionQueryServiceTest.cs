using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

public class SessionQueryServiceTest
{
    private const long OwnerId = 7;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IChatSessionRepository<ChatSession>> _sessionRepoMock = new();
    private readonly Mock<IChatMessageRepository<ChatMessage>> _messageRepoMock = new();
    private readonly Mock<ITelegramChatRepository<TelegramChat>> _telegramRepoMock = new();
    private readonly SessionQueryService _sut;

    public SessionQueryServiceTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<McpProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        _sut = new SessionQueryService(_agentRepoMock.Object, _sessionRepoMock.Object, _messageRepoMock.Object, _telegramRepoMock.Object, mapper);

        _agentRepoMock.Setup(r => r.GetByIdAsync(10, OwnerId)).ReturnsAsync(new Agent { AgentId = 10, OwnerUserId = OwnerId });
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);
        _telegramRepoMock.Setup(r => r.GetSessionIdsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(new HashSet<long>());
    }

    [Fact]
    public async Task GetSessionsAsync_ShouldThrowNotFound_WhenTheAgentBelongsToAnotherOwner()
    {
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetSessionsAsync(99, OwnerId, 1, 20));

        Assert.Equal("Agente nao encontrado", ex.Message);
        _sessionRepoMock.Verify(r => r.GetByAgentIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionsAsync_ShouldCapThePageSizeAt100_AndComputeMaxPage()
    {
        _sessionRepoMock.Setup(r => r.GetByAgentIdAsync(10, 1, 100)).ReturnsAsync(new List<ChatSession>());
        _sessionRepoMock.Setup(r => r.CountByAgentIdAsync(10)).ReturnsAsync(250);

        var result = await _sut.GetSessionsAsync(10, OwnerId, 1, 500);

        Assert.Equal(250, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.MaxPage);
        _sessionRepoMock.Verify(r => r.GetByAgentIdAsync(10, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetSessionsAsync_ShouldMarkTelegramSessions_AndCountMessages()
    {
        _sessionRepoMock.Setup(r => r.GetByAgentIdAsync(10, 1, 20)).ReturnsAsync(
        [
            new ChatSession { ChatSessionId = 1, AgentId = 10, UserName = "Ana" },
            new ChatSession { ChatSessionId = 2, AgentId = 10, UserName = "Bia" }
        ]);
        _sessionRepoMock.Setup(r => r.CountByAgentIdAsync(10)).ReturnsAsync(2);
        _messageRepoMock.Setup(r => r.CountBySessionIdAsync(1)).ReturnsAsync(5);
        _messageRepoMock.Setup(r => r.CountBySessionIdAsync(2)).ReturnsAsync(0);
        _telegramRepoMock.Setup(r => r.GetSessionIdsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(new HashSet<long> { 2 });

        var result = await _sut.GetSessionsAsync(10, OwnerId, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("web", result.Items[0].Channel);
        Assert.Equal(5, result.Items[0].MessageCount);
        Assert.Equal("telegram", result.Items[1].Channel);
        Assert.Equal(1, result.MaxPage);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldThrowSessionNotFound_WhenTheSessionBelongsToAnotherOwnersAgent()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(new ChatSession { ChatSessionId = 50, AgentId = 99 });

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetMessagesAsync(50, OwnerId, 1, 50));

        Assert.Equal("Sessao nao encontrada", ex.Message);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldThrowSessionNotFound_WhenTheSessionDoesNotExist()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((ChatSession?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetMessagesAsync(404, OwnerId, 1, 50));
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldMapRoles_AndCapThePageSizeAt200()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ChatSession { ChatSessionId = 1, AgentId = 10 });
        _messageRepoMock.Setup(r => r.GetBySessionIdAsync(1, 1, 200)).ReturnsAsync(
        [
            new ChatMessage { ChatMessageId = 1, SenderType = SenderType.User, Content = "Oi" },
            new ChatMessage { ChatMessageId = 2, SenderType = SenderType.Assistant, Content = "Ola" }
        ]);
        _messageRepoMock.Setup(r => r.CountBySessionIdAsync(1)).ReturnsAsync(2);

        var result = await _sut.GetMessagesAsync(1, OwnerId, 1, 999);

        Assert.Equal("visitante", result.Items[0].Role);
        Assert.Equal("agente", result.Items[1].Role);
        Assert.Equal(2, result.Total);
        _messageRepoMock.Verify(r => r.GetBySessionIdAsync(1, 1, 200), Times.Once);
    }

    [Fact]
    public async Task GetSessionPageAsync_ShouldUseTheDefaultPageSize_WhenZero()
    {
        _sessionRepoMock.Setup(r => r.GetByAgentIdAsync(10, 1, 20)).ReturnsAsync(new List<ChatSession>());
        _sessionRepoMock.Setup(r => r.CountByAgentIdAsync(10)).ReturnsAsync(0);

        var page = await _sut.GetSessionPageAsync(10, OwnerId, 0, 0);

        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }
}
