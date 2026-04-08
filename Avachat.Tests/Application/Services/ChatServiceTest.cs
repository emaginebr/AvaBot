using Xunit;
using Moq;
using Avachat.Application.Services;
using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Avachat.Tests.Application.Services;

public class ChatServiceTest
{
    private readonly Mock<SearchService> _searchServiceMock;
    private readonly Mock<IOpenAIService> _openAIServiceMock;
    private readonly Mock<IChatSessionRepository<ChatSession>> _sessionRepoMock;
    private readonly Mock<IChatMessageRepository<ChatMessage>> _messageRepoMock;
    private readonly ChatService _sut;

    public ChatServiceTest()
    {
        var esServiceMock = new Mock<IElasticsearchService>();
        _openAIServiceMock = new Mock<IOpenAIService>();
        _searchServiceMock = new Mock<SearchService>(esServiceMock.Object, _openAIServiceMock.Object);
        _sessionRepoMock = new Mock<IChatSessionRepository<ChatSession>>();
        _messageRepoMock = new Mock<IChatMessageRepository<ChatMessage>>();

        var configData = new Dictionary<string, string?>
        {
            { "Chat:MaxHistoryMessages", "10" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _sut = new ChatService(
            _searchServiceMock.Object,
            _openAIServiceMock.Object,
            _sessionRepoMock.Object,
            _messageRepoMock.Object,
            configuration,
            NullLogger<ChatService>.Instance);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateSession_WithCorrectProperties()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);

        // Act
        var result = await _sut.CreateSessionAsync(1, "John", "john@test.com", "123456");

        // Assert
        Assert.Equal(1, result.AgentId);
        Assert.Equal("John", result.UserName);
        Assert.Equal("john@test.com", result.UserEmail);
        Assert.Equal("123456", result.UserPhone);
        _sessionRepoMock.Verify(r => r.CreateAsync(It.IsAny<ChatSession>()), Times.Once);
    }

    [Fact]
    public async Task SaveMessageAsync_ShouldCreateMessage_WithCorrectSenderType()
    {
        // Arrange
        _messageRepoMock.Setup(r => r.CreateAsync(It.IsAny<ChatMessage>()))
            .ReturnsAsync((ChatMessage m) => m);

        // Act
        var result = await _sut.SaveMessageAsync(1, SenderType.User, "Hello");

        // Assert
        Assert.Equal(1, result.ChatSessionId);
        Assert.Equal(SenderType.User, result.SenderType);
        Assert.Equal("Hello", result.Content);
    }

    [Fact]
    public async Task EndSessionAsync_ShouldSetEndedAt_WhenSessionExists()
    {
        // Arrange
        var session = new ChatSession { ChatSessionId = 1, EndedAt = null };
        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _sessionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);

        // Act
        await _sut.EndSessionAsync(1);

        // Assert
        Assert.NotNull(session.EndedAt);
        _sessionRepoMock.Verify(r => r.UpdateAsync(It.Is<ChatSession>(s => s.EndedAt != null)), Times.Once);
    }

    [Fact]
    public async Task EndSessionAsync_ShouldDoNothing_WhenSessionNotFound()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ChatSession?)null);

        // Act
        await _sut.EndSessionAsync(999);

        // Assert
        _sessionRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ChatSession>()), Times.Never);
    }
}
