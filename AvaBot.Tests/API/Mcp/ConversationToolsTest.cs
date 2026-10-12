using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Mcp;
using AvaBot.API.Mcp.Tools;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Mcp;

public class ConversationToolsTest
{
    private const long OwnerId = McpTestContext.UserId;
    private const long AgentId = 10;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IChatSessionRepository<ChatSession>> _sessionRepoMock = new();
    private readonly Mock<IChatMessageRepository<ChatMessage>> _messageRepoMock = new();
    private readonly Mock<ITelegramChatRepository<TelegramChat>> _telegramRepoMock = new();
    private readonly Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>> _queryLogRepoMock = new();
    private readonly Mock<IOpenAIService> _openAIMock = new();
    private readonly McpCaller _caller = new(OwnerId, McpTestContext.KeyId, "Claude");
    private readonly ConversationTools _sut;

    public ConversationToolsTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        expr.AddProfile<McpProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var esMock = new Mock<IElasticsearchService>();
        var protectorMock = new Mock<ISecretProtector>();
        var clientMock = new Mock<IPowerBIClient>();
        var configRepoMock = new Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>>();
        var datasetRepoMock = new Mock<IPowerBIDatasetRepository<PowerBIDataset>>();

        var agentService = new AgentService(_agentRepoMock.Object, esMock.Object, protectorMock.Object, mapper);
        var sessionQuery = new SessionQueryService(_agentRepoMock.Object, _sessionRepoMock.Object, _messageRepoMock.Object, _telegramRepoMock.Object, mapper);
        var powerBIService = new PowerBIService(_agentRepoMock.Object, configRepoMock.Object, datasetRepoMock.Object, _queryLogRepoMock.Object,
            clientMock.Object, protectorMock.Object, new PowerBISchemaBuilder(clientMock.Object), NullLogger<PowerBIService>.Instance);
        var toolProvider = new PowerBIToolProvider(configRepoMock.Object, datasetRepoMock.Object, _queryLogRepoMock.Object, clientMock.Object,
            protectorMock.Object, config, NullLogger<PowerBIToolProvider>.Instance);
        var chatService = new ChatService(new SearchService(esMock.Object), _openAIMock.Object, _sessionRepoMock.Object, _messageRepoMock.Object,
            _agentRepoMock.Object, toolProvider, config, NullLogger<ChatService>.Instance);

        _sut = new ConversationTools(sessionQuery, agentService, powerBIService, chatService, NullLogger<ConversationTools>.Instance);

        var agent = new Agent { AgentId = AgentId, OwnerUserId = OwnerId, Slug = "agente", Name = "Agente", ChatModel = "gpt-4o", SystemPrompt = "p" };
        _agentRepoMock.Setup(r => r.GetByIdAsync(AgentId, OwnerId)).ReturnsAsync(agent);
        _agentRepoMock.Setup(r => r.GetBySlugAsync("agente", OwnerId)).ReturnsAsync(agent);
        _telegramRepoMock.Setup(r => r.GetSessionIdsAsync(It.IsAny<IEnumerable<long>>())).ReturnsAsync(new HashSet<long>());
    }

    [Fact]
    public async Task TestarAgente_ShouldRejectAnInvalidHistoryRole_WithTheControllerMessage_WithoutCallingTheModel()
    {
        var history = new List<AgentTestMessageInfo> { new() { Role = "system", Content = "x" } };

        var result = await _sut.TestarAgenteAsync(_caller, AgentId, "pergunta", history);

        Assert.True(result.IsError);
        Assert.Equal("Historico invalido: item 1 tem role 'system' (use 'user' ou 'assistant')", McpTestContext.Text(result));
        _openAIMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestarAgente_ShouldRejectAnEmptyQuestion()
    {
        var result = await _sut.TestarAgenteAsync(_caller, AgentId, "  ", null);

        Assert.True(result.IsError);
        Assert.Equal("O parametro 'question' e obrigatorio", McpTestContext.Text(result));
    }

    [Fact]
    public async Task TestarAgente_ShouldAnswerNotFound_ForAForeignAgent()
    {
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.TestarAgenteAsync(_caller, 99, "pergunta", null);

        Assert.True(result.IsError);
        Assert.Equal("Agente nao encontrado", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ListarConsultasBi_ShouldRejectAnUnknownStatus()
    {
        var result = await _sut.ListarConsultasBiAsync(_caller, AgentId, 1, 20, "falha");

        Assert.True(result.IsError);
        Assert.Equal("Status invalido. Use success, error ou timeout", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ListarConsultasBi_ShouldPassTheParsedStatus_ToTheService()
    {
        _queryLogRepoMock.Setup(r => r.GetPagedByAgentAsync(AgentId, 1, 20, (int)PowerBIQueryStatus.Error))
            .ReturnsAsync((new List<PowerBIQueryLog> { new() { PowerBIQueryLogId = 5, Status = PowerBIQueryStatus.Error, ErrorMessage = "boom" } }, 1));

        var result = await _sut.ListarConsultasBiAsync(_caller, AgentId, 1, 20, "error");

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("\"powerBIQueryLogId\":5", McpTestContext.Text(result));
        _queryLogRepoMock.Verify(r => r.GetPagedByAgentAsync(AgentId, 1, 20, (int)PowerBIQueryStatus.Error), Times.Once);
    }

    [Fact]
    public async Task ListarSessoes_ShouldPassPageAndPageSize_ToTheService()
    {
        _sessionRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId, 2, 10)).ReturnsAsync(new List<ChatSession>());
        _sessionRepoMock.Setup(r => r.CountByAgentIdAsync(AgentId)).ReturnsAsync(15);

        var result = await _sut.ListarSessoesAsync(_caller, AgentId, 2, 10);

        Assert.NotEqual(true, result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"total\":15", text);
        Assert.Contains("\"page\":2", text);
        Assert.Contains("\"maxPage\":2", text);
        _sessionRepoMock.Verify(r => r.GetByAgentIdAsync(AgentId, 2, 10), Times.Once);
    }

    [Fact]
    public async Task ListarMensagens_ShouldAnswerSessionNotFound_ForAForeignSession()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(new ChatSession { ChatSessionId = 50, AgentId = 99 });
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.ListarMensagensAsync(_caller, 50, 1, 50);

        Assert.True(result.IsError);
        Assert.Equal("Sessao nao encontrada", McpTestContext.Text(result));
    }
}
