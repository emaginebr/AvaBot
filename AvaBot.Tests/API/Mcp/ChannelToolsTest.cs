using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Mcp;
using AvaBot.API.Mcp.Tools;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Mcp;

public class ChannelToolsTest
{
    private const long OwnerId = McpTestContext.UserId;
    private const long AgentId = 10;
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IWppConnectService> _wppMock = new();
    private readonly McpCaller _caller = new(OwnerId, McpTestContext.KeyId, "Claude");
    private readonly ChannelTools _sut;

    public ChannelToolsTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var esMock = new Mock<IElasticsearchService>();
        var sessionRepoMock = new Mock<IChatSessionRepository<ChatSession>>();
        var messageRepoMock = new Mock<IChatMessageRepository<ChatMessage>>();
        var queryLogRepoMock = new Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>>();
        var protectorMock = new Mock<ISecretProtector>();

        var agentService = new AgentService(_agentRepoMock.Object, esMock.Object, protectorMock.Object, mapper);
        var toolProvider = new PowerBIToolProvider(
            new Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>>().Object,
            new Mock<IPowerBIDatasetRepository<PowerBIDataset>>().Object,
            queryLogRepoMock.Object, new Mock<IPowerBIClient>().Object, protectorMock.Object, config, NullLogger<PowerBIToolProvider>.Instance);
        var chatService = new ChatService(new SearchService(esMock.Object), new Mock<IOpenAIService>().Object, sessionRepoMock.Object,
            messageRepoMock.Object, _agentRepoMock.Object, toolProvider, config, NullLogger<ChatService>.Instance);
        var telegramService = new TelegramService(new Mock<ITelegramChatRepository<TelegramChat>>().Object, sessionRepoMock.Object,
            _agentRepoMock.Object, chatService, agentService, NullLogger<TelegramService>.Instance);
        var whatsappService = new WhatsappService(_wppMock.Object, _agentRepoMock.Object, agentService, chatService, config, NullLogger<WhatsappService>.Instance);

        _sut = new ChannelTools(agentService, telegramService, whatsappService, NullLogger<ChannelTools>.Instance);
    }

    private Agent ArrangeAgent(string? telegramToken)
    {
        var agent = new Agent { AgentId = AgentId, OwnerUserId = OwnerId, Name = "Suporte", Slug = "suporte", TelegramBotName = "suporte_bot", TelegramBotToken = telegramToken, TelegramWebhookSecret = "SEGREDO" };
        _agentRepoMock.Setup(r => r.GetByIdAsync(AgentId, OwnerId)).ReturnsAsync(agent);
        _agentRepoMock.Setup(r => r.GetBySlugAsync("suporte", OwnerId)).ReturnsAsync(agent);
        return agent;
    }

    [Fact]
    public async Task ObterTelegram_ShouldReportBotNotConfigured_WithoutCallingTelegram_WhenThereIsNoToken()
    {
        ArrangeAgent(null);

        var result = await _sut.ObterTelegramAsync(_caller, AgentId);

        Assert.NotEqual(true, result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"botName\":\"suporte_bot\"", text);
        Assert.Contains("\"botConfigured\":false", text);
        Assert.Contains("\"webhookRegistered\":false", text);
        Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SEGREDO", text);
    }

    [Fact]
    public async Task RegistrarWebhookTelegram_ShouldGuideTheUser_WhenThereIsNoToken()
    {
        ArrangeAgent(null);

        var result = await _sut.RegistrarWebhookTelegramAsync(_caller, AgentId);

        Assert.True(result.IsError);
        Assert.Equal(ChannelTools.MissingTokenMessage("Suporte"), McpTestContext.Text(result));
    }

    [Fact]
    public async Task ObterTelegram_ShouldAnswerNotFound_ForAForeignAgent()
    {
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.ObterTelegramAsync(_caller, 99);

        Assert.True(result.IsError);
        Assert.Equal("Agente nao encontrado", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ObterWhatsapp_ShouldReturnStatus_WithoutTheToken()
    {
        var agent = ArrangeAgent(null);
        agent.WhatsappToken = "WPP-TOKEN";
        _wppMock.Setup(w => w.GetStatusAsync("suporte")).ReturnsAsync("CONNECTED");

        var result = await _sut.ObterWhatsappAsync(_caller, AgentId);

        Assert.NotEqual(true, result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"status\":\"CONNECTED\"", text);
        Assert.Contains("\"isConnected\":true", text);
        Assert.DoesNotContain("WPP-TOKEN", text);
    }

    [Fact]
    public async Task ObterWhatsapp_ShouldReportNotStarted_WithoutCallingWppConnect_WhenThereIsNoSession()
    {
        ArrangeAgent(null);

        var result = await _sut.ObterWhatsappAsync(_caller, AgentId);

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("\"status\":\"NOT_STARTED\"", McpTestContext.Text(result));
        Assert.Contains("\"isConnected\":false", McpTestContext.Text(result));
        _wppMock.Verify(w => w.GetStatusAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ObterWhatsapp_ShouldTurnAWppConnectFailure_IntoAReadableMessage()
    {
        var agent = ArrangeAgent(null);
        agent.WhatsappToken = "WPP-TOKEN";
        _wppMock.Setup(w => w.GetStatusAsync("suporte")).ThrowsAsync(new HttpRequestException("connection refused"));

        var result = await _sut.ObterWhatsappAsync(_caller, AgentId);

        Assert.True(result.IsError);
        Assert.Equal("Nao foi possivel consultar o WhatsApp: connection refused", McpTestContext.Text(result));
    }
}
