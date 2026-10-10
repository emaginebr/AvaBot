using Xunit;
using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using AvaBot.API.Controllers;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Controllers;

public class AgentControllerTest
{
    private readonly Mock<IAgentRepository<Agent>> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly AgentService _agentService;
    private readonly SearchService _searchService;
    private readonly ChatService _chatService;
    private const long OwnerId = 7;
    private readonly AgentController _sut;

    public AgentControllerTest()
    {
        _repositoryMock = new Mock<IAgentRepository<Agent>>();
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        _mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();
        var esServiceMock = new Mock<IElasticsearchService>();
        var openAIMock = new Mock<IOpenAIService>();
        _agentService = new AgentService(_repositoryMock.Object, esServiceMock.Object, new Mock<ISecretProtector>().Object, _mapper);
        _searchService = new SearchService(esServiceMock.Object);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var powerBIToolProvider = new PowerBIToolProvider(
            new Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>>().Object,
            new Mock<IPowerBIDatasetRepository<PowerBIDataset>>().Object,
            new Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>>().Object,
            new Mock<IPowerBIClient>().Object,
            new Mock<ISecretProtector>().Object,
            config,
            NullLogger<PowerBIToolProvider>.Instance);
        _chatService = new ChatService(
            _searchService, openAIMock.Object,
            new Mock<IChatSessionRepository<ChatSession>>().Object,
            new Mock<IChatMessageRepository<ChatMessage>>().Object,
            _repositoryMock.Object,
            powerBIToolProvider,
            config, NullLogger<ChatService>.Instance);
        _sut = new AgentController(_agentService, _searchService, _chatService, openAIMock.Object, _mapper).WithUser(OwnerId);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithAgentList()
    {
        var agents = new List<Agent>
        {
            new() { AgentId = 1, Name = "Agent 1", Slug = "agent-1", SystemPrompt = "Prompt" }
        };
        _repositoryMock.Setup(r => r.GetAllByOwnerAsync(OwnerId)).ReturnsAsync(agents);

        var result = await _sut.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<List<AgentInfo>>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.Single(response.Dados!);
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnNotFound_WhenAgentNotExists()
    {
        _repositoryMock.Setup(r => r.GetBySlugAsync("nonexistent")).ReturnsAsync((Agent?)null);

        var result = await _sut.GetBySlug("nonexistent");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnOk_WhenAgentExists()
    {
        var agent = new Agent { AgentId = 1, Slug = "test", Name = "Test", SystemPrompt = "P" };
        _repositoryMock.Setup(r => r.GetBySlugAsync("test")).ReturnsAsync(agent);

        var result = await _sut.GetBySlug("test");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<AgentInfo>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.Equal("test", response.Dados!.Slug);
    }

    [Fact]
    public async Task GetChatConfig_ShouldReturnNotFound_WhenAgentNotExists()
    {
        _repositoryMock.Setup(r => r.GetBySlugAsync("missing")).ReturnsAsync((Agent?)null);
        Assert.IsType<NotFoundObjectResult>(await _sut.GetChatConfig("missing"));
    }

    [Fact]
    public async Task GetChatConfig_ShouldReturnFailure_WhenAgentInactive()
    {
        var agent = new Agent { AgentId = 1, Slug = "inactive", Status = 0 };
        _repositoryMock.Setup(r => r.GetBySlugAsync("inactive")).ReturnsAsync(agent);

        var result = await _sut.GetChatConfig("inactive");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<object>>(okResult.Value);
        Assert.False(response.Sucesso);
    }

    [Fact]
    public async Task GetChatConfig_ShouldReturnConfig_WhenAgentActive()
    {
        var agent = new Agent
        {
            AgentId = 1, Slug = "active", Name = "Active Agent", Status = 1,
            CollectName = true, CollectEmail = true, CollectPhone = false
        };
        _repositoryMock.Setup(r => r.GetBySlugAsync("active")).ReturnsAsync(agent);

        var result = await _sut.GetChatConfig("active");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<AgentChatConfigInfo>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.True(response.Dados!.CollectName);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WithAutoGeneratedSlug()
    {
        var info = new AgentInsertInfo { Name = "New Agent", SystemPrompt = "P" };
        _repositoryMock.Setup(r => r.SlugExistsAsync("new-agent", null)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => { a.AgentId = 1; return a; });

        var result = await _sut.Create(info);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var response = Assert.IsType<Result<AgentInfo>>(createdResult.Value);
        Assert.True(response.Sucesso);
        Assert.Equal("new-agent", response.Dados!.Slug);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenAgentNotExists()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.Update(999, new AgentInsertInfo { Name = "X", SystemPrompt = "P" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenAgentExists()
    {
        var existing = new Agent { AgentId = 1, Name = "Old", Slug = "old" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, OwnerId)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.SlugExistsAsync("new-name", 1L)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _sut.Update(1, new AgentInsertInfo { Name = "New Name", SystemPrompt = "P" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<AgentInfo>>(okResult.Value);
        Assert.True(response.Sucesso);
        Assert.Equal("new-name", response.Dados!.Slug);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenAgentNotExists()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999, OwnerId)).ReturnsAsync((Agent?)null);
        Assert.IsType<NotFoundObjectResult>(await _sut.Delete(999));
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenAgentExists()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(1, OwnerId)).ReturnsAsync(new Agent { AgentId = 1 });

        var result = await _sut.Delete(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Result<object>>(okResult.Value);
    }

    [Fact]
    public async Task ToggleStatus_ShouldReturnNotFound_WhenAgentNotExists()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999, OwnerId)).ReturnsAsync((Agent?)null);
        Assert.IsType<NotFoundObjectResult>(await _sut.ToggleStatus(999));
    }

    [Fact]
    public async Task ToggleStatus_ShouldReturnOk_WhenAgentExists()
    {
        var agent = new Agent { AgentId = 1, Status = 1, Name = "A", Slug = "a" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, OwnerId)).ReturnsAsync(agent);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _sut.ToggleStatus(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<Result<AgentInfo>>(okResult.Value);
        Assert.True(response.Sucesso);
    }
    [Fact]
    public async Task Create_ShouldStoreTheOwnerFromTheToken()
    {
        Agent? created = null;
        _repositoryMock.Setup(r => r.SlugExistsAsync("mine", null)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .Callback<Agent>(a => created = a)
            .ReturnsAsync((Agent a) => { a.AgentId = 1; return a; });

        await _sut.Create(new AgentInsertInfo { Name = "Mine", SystemPrompt = "P" });

        Assert.Equal(OwnerId, created!.OwnerUserId);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenAgentBelongsToAnotherOwner()
    {
        // FR-011: mesma resposta de agente inexistente
        _repositoryMock.Setup(r => r.GetByIdAsync(5, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.Update(5, new AgentInsertInfo { Name = "X", SystemPrompt = "P" });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Agente nao encontrado", Assert.IsType<Result<object>>(notFound.Value).Mensagem);
    }

    [Fact]
    public async Task Search_ShouldReturnNotFound_WhenAgentBelongsToAnotherOwner()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(5, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.Search(5, "pergunta");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Agente nao encontrado", Assert.IsType<Result<object>>(notFound.Value).Mensagem);
    }

    [Fact]
    public async Task GetAll_ShouldReturnUnauthorized_WhenTheTokenHasNoUserId()
    {
        var result = await _sut.WithoutUser().GetAll();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}