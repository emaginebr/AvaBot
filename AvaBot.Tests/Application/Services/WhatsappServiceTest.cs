using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

/// <summary>
/// Ordem do webhook com o controle de acesso (feature 018, contracts/whatsapp-webhook-flow.md §1).
/// ChatService e concreto: "chegou na IA" e verificado pela criacao da sessao (IChatSessionRepository.CreateAsync),
/// que acontece antes de qualquer chamada ao modelo. O erro seguinte (modelo nao configurado) e engolido pelo webhook.
/// </summary>
public class WhatsappServiceTest
{
    private const string Slug = "suporte";
    private readonly Mock<IWppConnectService> _wppMock = new();
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IChatSessionRepository<ChatSession>> _sessionRepoMock = new();
    private readonly Mock<IOpenAIService> _openAIMock = new();
    private readonly WhatsappAccessTestContext _access = new();
    private readonly WhatsappService _sut;
    private readonly Agent _agent = new()
    {
        AgentId = WhatsappAccessTestContext.AgentId,
        Name = "Suporte",
        Slug = Slug,
        Status = 1,
        WhatsappToken = "token",
        ChatModel = "gpt-4o",
        SystemPrompt = "Voce e um assistente."
    };

    public WhatsappServiceTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var esMock = new Mock<IElasticsearchService>();
        var protectorMock = new Mock<ISecretProtector>();

        var agentService = new AgentService(_agentRepoMock.Object, esMock.Object, protectorMock.Object, mapper);
        var toolProvider = new PowerBIToolProvider(
            new Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>>().Object,
            new Mock<IPowerBIDatasetRepository<PowerBIDataset>>().Object,
            new Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>>().Object, new Mock<IPowerBIClient>().Object, protectorMock.Object, config,
            NullLogger<PowerBIToolProvider>.Instance);
        var chatService = new ChatService(new SearchService(esMock.Object), _openAIMock.Object, _sessionRepoMock.Object,
            new Mock<IChatMessageRepository<ChatMessage>>().Object, _agentRepoMock.Object, toolProvider, config, NullLogger<ChatService>.Instance);

        _agentRepoMock.Setup(r => r.GetBySlugAsync(Slug)).ReturnsAsync(_agent);
        _wppMock.Setup(w => w.GetStatusAsync(Slug)).ReturnsAsync("CONNECTED");
        _sessionRepoMock.Setup(r => r.CreateAsync(It.IsAny<ChatSession>())).ReturnsAsync((ChatSession s) => { s.ChatSessionId = 1; return s; });

        _sut = new WhatsappService(_wppMock.Object, _agentRepoMock.Object, agentService, chatService, _access.Service, config,
            NullLogger<WhatsappService>.Instance);
    }

    private static JsonElement Payload(string from, string body = "oi", string type = "chat", bool isGroup = false, string? author = null)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["event"] = "onmessage",
            ["session"] = Slug,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
            ["from"] = from,
            ["author"] = author,
            ["type"] = type,
            ["body"] = body,
            ["isGroupMsg"] = isGroup,
            ["sender"] = new Dictionary<string, object?> { ["pushname"] = "Maria" }
        });
        return JsonDocument.Parse(json).RootElement;
    }

    private void VerifyReachedChat(Times times) => _sessionRepoMock.Verify(r => r.CreateAsync(It.IsAny<ChatSession>()), times);

    [Fact]
    public async Task ModeAll_ShouldGoStraightToChat_WithoutAccessChecks()
    {
        await _sut.ProcessWebhookAsync(Slug, Payload("5511987654321@c.us"));

        VerifyReachedChat(Times.Once());
        _access.Contacts.VerifyNoOtherCalls();
        _wppMock.Verify(w => w.GetPhoneByLidAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ModeAll_UnsupportedType_ShouldStillSendTheTypeRejection()
    {
        await _sut.ProcessWebhookAsync(Slug, Payload("5511987654321@c.us", "", "ptt"));

        _wppMock.Verify(w => w.SendMessageAsync(Slug, "5511987654321", "Desculpe, eu so consigo processar mensagens de texto."), Times.Once);
        VerifyReachedChat(Times.Never());
    }

    [Fact]
    public async Task Restricted_Unauthorized_ShouldSendOffer_AndNeverReachChat()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.Restricted;

        await _sut.ProcessWebhookAsync(Slug, Payload("5511987654321@c.us"));

        _wppMock.Verify(w => w.SendMessageAsync(Slug, "5511987654321", WhatsappAccessDefaults.OfferMessage), Times.Once);
        VerifyReachedChat(Times.Never());
        _openAIMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Restricted_Ignore_ShouldSendNothing()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.Restricted;
        _access.ArrangeState("5511987654321@c.us", _access.Time.UtcNow.AddMinutes(-5), _access.Time.UtcNow.AddMinutes(-5));

        await _sut.ProcessWebhookAsync(Slug, Payload("5511987654321@c.us", "oi de novo"));

        _wppMock.Verify(w => w.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        VerifyReachedChat(Times.Never());
    }

    [Fact]
    public async Task Restricted_AuthorizedContact_ShouldReachChat()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.Restricted;
        _access.Contacts.Setup(c => c.FindBySenderAsync(_agent.AgentId, "551187654321", "5511987654321@c.us"))
            .ReturnsAsync(new WhatsappContact { WhatsappContactId = 1, Name = "Ana", WhatsappId = "5511987654321@c.us" });

        await _sut.ProcessWebhookAsync(Slug, Payload("5511987654321@c.us"));

        VerifyReachedChat(Times.Once());
    }

    [Fact]
    public async Task Restricted_UnsupportedTypeFromUnauthorized_ShouldGetOffer_NotTypeRejection()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.Restricted;

        await _sut.ProcessWebhookAsync(Slug, Payload("5511987654321@c.us", "", "ptt"));

        _wppMock.Verify(w => w.SendMessageAsync(Slug, "5511987654321", WhatsappAccessDefaults.OfferMessage), Times.Once);
        _wppMock.Verify(w => w.SendMessageAsync(Slug, It.IsAny<string>(), "Desculpe, eu so consigo processar mensagens de texto."), Times.Never);
    }

    [Fact]
    public async Task Restricted_LidSender_ShouldResolvePhoneBeforeEvaluating()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.Restricted;
        _wppMock.Setup(w => w.GetPhoneByLidAsync(Slug, "998877@lid")).ReturnsAsync("5511987654321");
        _access.Contacts.Setup(c => c.FindBySenderAsync(_agent.AgentId, "551187654321", "998877@lid"))
            .ReturnsAsync(new WhatsappContact { WhatsappContactId = 1, Name = "Ana", PhoneMatchKey = "551187654321" });

        await _sut.ProcessWebhookAsync(Slug, Payload("998877@lid"));

        _wppMock.Verify(w => w.GetPhoneByLidAsync(Slug, "998877@lid"), Times.Once);
        VerifyReachedChat(Times.Once());
    }

    [Fact]
    public async Task Restricted_GroupMention_ShouldCheckTheAuthor_AndIgnoreUnauthorized()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.Restricted;
        _wppMock.Setup(w => w.GetBotLidAsync(Slug, "120363@g.us")).ReturnsAsync("bot@lid");
        var json = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["event"] = "onmessage",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
            ["from"] = "120363@g.us",
            ["author"] = "5511987654321@c.us",
            ["type"] = "chat",
            ["body"] = "@bot oi",
            ["isGroupMsg"] = true,
            ["mentionedJidList"] = new[] { "bot@lid" }
        });

        await _sut.ProcessWebhookAsync(Slug, JsonDocument.Parse(json).RootElement);

        _access.Contacts.Verify(c => c.FindBySenderAsync(_agent.AgentId, "551187654321", "5511987654321@c.us"), Times.Once);
        _wppMock.Verify(w => w.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        VerifyReachedChat(Times.Never());
    }
}
