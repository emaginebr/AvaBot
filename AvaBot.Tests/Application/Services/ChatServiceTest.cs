using Xunit;
using Moq;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AvaBot.Tests.Application.Services;

public class ChatServiceTest
{
    private const long AgentId = 1;
    private const long SessionId = 2;

    private readonly Mock<SearchService> _searchServiceMock;
    private readonly Mock<IElasticsearchService> _esServiceMock;
    private readonly Mock<IOpenAIService> _openAIServiceMock;
    private readonly Mock<IChatSessionRepository<ChatSession>> _sessionRepoMock;
    private readonly Mock<IChatMessageRepository<ChatMessage>> _messageRepoMock;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock;
    private readonly Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>> _configRepoMock;
    private readonly Mock<IPowerBIDatasetRepository<PowerBIDataset>> _datasetRepoMock;
    private readonly Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>> _queryLogRepoMock;
    private readonly Mock<IPowerBIClient> _powerBIClientMock;
    private readonly Mock<ISecretProtector> _secretProtectorMock;
    private readonly IConfiguration _configuration;
    private readonly ChatService _sut;

    public ChatServiceTest()
    {
        _esServiceMock = new Mock<IElasticsearchService>();
        _openAIServiceMock = new Mock<IOpenAIService>();
        _searchServiceMock = new Mock<SearchService>(_esServiceMock.Object);
        _sessionRepoMock = new Mock<IChatSessionRepository<ChatSession>>();
        _messageRepoMock = new Mock<IChatMessageRepository<ChatMessage>>();
        _agentRepoMock = new Mock<IAgentRepository<Agent>>();
        _configRepoMock = new Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>>();
        _datasetRepoMock = new Mock<IPowerBIDatasetRepository<PowerBIDataset>>();
        _queryLogRepoMock = new Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>>();
        _powerBIClientMock = new Mock<IPowerBIClient>();
        _secretProtectorMock = new Mock<ISecretProtector>();

        var configData = new Dictionary<string, string?>
        {
            { "Chat:MaxHistoryMessages", "10" },
            { "PowerBI:MaxToolCallsPerMessage", "5" },
            { "PowerBI:MaxRows", "100" },
            { "PowerBI:QueryTimeoutSeconds", "30" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Provider real: o que muda entre os cenarios e a flag do agente e o conteo do repositorio.
        var toolProvider = new PowerBIToolProvider(
            _configRepoMock.Object,
            _datasetRepoMock.Object,
            _queryLogRepoMock.Object,
            _powerBIClientMock.Object,
            _secretProtectorMock.Object,
            _configuration,
            NullLogger<PowerBIToolProvider>.Instance);

        _sut = new ChatService(
            _searchServiceMock.Object,
            _openAIServiceMock.Object,
            _sessionRepoMock.Object,
            _messageRepoMock.Object,
            _agentRepoMock.Object,
            toolProvider,
            _configuration,
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

    [Fact]
    public async Task ProcessMessageAsync_ShouldUseStreamWithoutTools_WhenPowerBIIsDisabled()
    {
        // Arrange
        ArrangeChatPipeline();
        _agentRepoMock.Setup(r => r.GetByIdAsync(AgentId))
            .ReturnsAsync(new Agent { AgentId = AgentId, PowerBIEnabled = false });

        _openAIServiceMock
            .Setup(o => o.StreamChatCompletionAsync(
                AgentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatCompletionMessage>>(), It.IsAny<CancellationToken>()))
            .Returns(NoTokens);

        // Act
        await ConsumeAsync(_sut.ProcessMessageAsync(AgentId, SessionId, "gpt-4o", "Prompt", "Quanto exportamos?"));

        // Assert (SC-005: sem a flag, o caminho executado e o atual)
        _openAIServiceMock.Verify(o => o.StreamChatCompletionAsync(
            AgentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatCompletionMessage>>(), It.IsAny<CancellationToken>()), Times.Once);
        _openAIServiceMock.Verify(o => o.StreamChatCompletionWithToolsAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatCompletionMessage>>(),
            It.IsAny<IReadOnlyList<ChatToolDefinition>>(), It.IsAny<Func<ChatToolCall, CancellationToken, Task<string>>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldUseStreamWithToolsAndPowerBIPrompt_WhenPowerBIIsEnabled()
    {
        // Arrange
        ArrangeChatPipeline();
        ArrangeAgentWithUsableDataset();

        string? capturedPrompt = null;
        long capturedAgentId = 0;

        _openAIServiceMock
            .Setup(o => o.StreamChatCompletionWithToolsAsync(
                AgentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatCompletionMessage>>(),
                It.IsAny<IReadOnlyList<ChatToolDefinition>>(), It.IsAny<Func<ChatToolCall, CancellationToken, Task<string>>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((long agentId, string model, string systemPrompt, List<ChatCompletionMessage> messages,
                IReadOnlyList<ChatToolDefinition> tools, Func<ChatToolCall, CancellationToken, Task<string>> executor,
                int maxToolCalls, CancellationToken token) =>
            {
                capturedAgentId = agentId;
                capturedPrompt = systemPrompt;
                Assert.Equal(2, tools.Count);
                // D3/014: o teto do loop precisa caber o orcamento BI (5) mais a leitura de schema (1).
                Assert.Equal(6, maxToolCalls);
                return NoTokens();
            });

        // Act
        await ConsumeAsync(_sut.ProcessMessageAsync(AgentId, SessionId, "gpt-4o", "Prompt", "Quanto exportamos de tilapia em 2025?"));

        // Assert
        _openAIServiceMock.Verify(o => o.StreamChatCompletionAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ChatCompletionMessage>>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal(AgentId, capturedAgentId);

        Assert.NotNull(capturedPrompt);
        Assert.Contains(PowerBIToolset.PromptAddendum, capturedPrompt);
        Assert.Contains("DADOS DO POWER BI", capturedPrompt);
        Assert.Contains("ou nos dados retornados pelas ferramentas", capturedPrompt);
    }

    private void ArrangeChatPipeline()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(new ChatSession { ChatSessionId = SessionId });
        _messageRepoMock.Setup(r => r.GetRecentBySessionIdAsync(SessionId, It.IsAny<int>()))
            .ReturnsAsync(new List<ChatMessage>());
        _messageRepoMock.Setup(r => r.CreateAsync(It.IsAny<ChatMessage>()))
            .ReturnsAsync((ChatMessage m) => m);
        _esServiceMock.Setup(e => e.TextSearchAsync(AgentId, It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<string>());
    }

    private void ArrangeAgentWithUsableDataset()
    {
        var schema = new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new()
                {
                    Name = "Exportacoes",
                    Columns = new List<PowerBISchemaColumn>
                    {
                        new() { Name = "Data", DataType = "DateTime" }
                    }
                }
            }
        };

        _agentRepoMock.Setup(r => r.GetByIdAsync(AgentId))
            .ReturnsAsync(new Agent { AgentId = AgentId, PowerBIEnabled = true });

        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new AgentPowerBIConfig
            {
                AgentId = AgentId,
                TenantId = "tenant",
                ClientId = "client",
                ClientSecretEncrypted = "cipher"
            });

        _secretProtectorMock.Setup(p => p.Unprotect("cipher")).Returns("secret");

        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new List<PowerBIDataset>
            {
                new()
                {
                    PowerBIDatasetId = 10,
                    AgentId = AgentId,
                    Name = "Comercio Internacional",
                    ToolKey = "comercio_internacional",
                    WorkspaceId = "workspace",
                    DatasetId = "dataset",
                    SchemaJson = PowerBISchema.Serialize(schema),
                    SchemaStatus = PowerBISchemaStatus.Generated
                }
            });
    }

    private static async IAsyncEnumerable<string> NoTokens()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async Task ConsumeAsync(IAsyncEnumerable<string> tokens)
    {
        await foreach (var _ in tokens)
        {
        }
    }
}
