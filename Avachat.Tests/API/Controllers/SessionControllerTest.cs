using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Avachat.API.Controllers;
using Avachat.Application.Profiles;
using Avachat.Application.Services;
using Avachat.Domain.Enums;
using Avachat.Domain.Models;
using Avachat.DTO;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Tests.API.Controllers;

public class SessionControllerTest
{
    private readonly Mock<IChatSessionRepository<ChatSession>> _sessionRepoMock;
    private readonly Mock<IChatMessageRepository<ChatMessage>> _messageRepoMock;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock;
    private readonly IMapper _mapper;
    private readonly SessionController _sut;

    public SessionControllerTest()
    {
        _sessionRepoMock = new Mock<IChatSessionRepository<ChatSession>>();
        _messageRepoMock = new Mock<IChatMessageRepository<ChatMessage>>();
        _agentRepoMock = new Mock<IAgentRepository<Agent>>();

        var expr = new MapperConfigurationExpression();
        expr.AddProfile<ChatSessionProfile>();
        expr.AddProfile<ChatMessageProfile>();
        expr.AddProfile<AgentProfile>();
        _mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var agentService = new AgentService(_agentRepoMock.Object, _mapper);

        var esServiceMock = new Mock<IElasticsearchService>();
        var openAIMock = new Mock<IOpenAIService>();
        var searchService = new SearchService(esServiceMock.Object, openAIMock.Object);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var chatService = new ChatService(searchService, openAIMock.Object, _sessionRepoMock.Object, _messageRepoMock.Object, config, NullLogger<ChatService>.Instance);

        _sut = new SessionController(_sessionRepoMock.Object, _messageRepoMock.Object, agentService, chatService, _mapper);
    }

    [Fact]
    public async Task GetSessions_ShouldReturnOk_WithPaginatedResult()
    {
        // Arrange
        var sessions = new List<ChatSession>
        {
            new() { ChatSessionId = 1, AgentId = 10, UserName = "User" }
        };
        _sessionRepoMock.Setup(r => r.GetByAgentIdAsync(10, 1, 20)).ReturnsAsync(sessions);
        _sessionRepoMock.Setup(r => r.CountByAgentIdAsync(10)).ReturnsAsync(1);
        _messageRepoMock.Setup(r => r.CountBySessionIdAsync(1)).ReturnsAsync(5);

        // Act
        var result = await _sut.GetSessions(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<PaginatedResult<ChatSessionInfo>>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.Single(response.Dados!.Items);
        Assert.Equal(5, response.Dados.Items[0].MessageCount);
    }

    [Fact]
    public async Task GetSessions_ShouldCapPageSize_At100()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.GetByAgentIdAsync(1, 1, 100)).ReturnsAsync(new List<ChatSession>());
        _sessionRepoMock.Setup(r => r.CountByAgentIdAsync(1)).ReturnsAsync(0);

        // Act
        var result = await _sut.GetSessions(1, maxPage: 500);

        // Assert
        _sessionRepoMock.Verify(r => r.GetByAgentIdAsync(1, 1, 100), Times.Once);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WithPaginatedMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { ChatMessageId = 1, ChatSessionId = 10, SenderType = SenderType.User, Content = "Hello" },
            new() { ChatMessageId = 2, ChatSessionId = 10, SenderType = SenderType.Assistant, Content = "Hi" }
        };
        _messageRepoMock.Setup(r => r.GetBySessionIdAsync(10, 1, 50)).ReturnsAsync(messages);
        _messageRepoMock.Setup(r => r.CountBySessionIdAsync(10)).ReturnsAsync(2);

        // Act
        var result = await _sut.GetMessages(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<PaginatedResult<ChatMessageInfo>>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.Equal(2, response.Dados!.Items.Count);
    }

    [Fact]
    public async Task GetMessages_ShouldCapPageSize_At200()
    {
        // Arrange
        _messageRepoMock.Setup(r => r.GetBySessionIdAsync(1, 1, 200)).ReturnsAsync(new List<ChatMessage>());
        _messageRepoMock.Setup(r => r.CountBySessionIdAsync(1)).ReturnsAsync(0);

        // Act
        var result = await _sut.GetMessages(1, maxPage: 999);

        // Assert
        _messageRepoMock.Verify(r => r.GetBySessionIdAsync(1, 1, 200), Times.Once);
    }
}
