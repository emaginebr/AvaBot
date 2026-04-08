using Xunit;
using Moq;
using AutoMapper;
using Avachat.Application.Profiles;
using Avachat.Application.Services;
using Avachat.Domain.Models;
using Avachat.DTO;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Tests.Application.Services;

public class AgentServiceTest
{
    private readonly Mock<IAgentRepository<Agent>> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly AgentService _sut;

    public AgentServiceTest()
    {
        _repositoryMock = new Mock<IAgentRepository<Agent>>();
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        _mapper = new MapperConfiguration(expr, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();
        _sut = new AgentService(_repositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAgents()
    {
        // Arrange
        var agents = new List<Agent>
        {
            new() { AgentId = 1, Name = "Agent 1", Slug = "agent-1" },
            new() { AgentId = 2, Name = "Agent 2", Slug = "agent-2" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(agents);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnAgent_WhenSlugExists()
    {
        // Arrange
        var agent = new Agent { AgentId = 1, Slug = "test-slug" };
        _repositoryMock.Setup(r => r.GetBySlugAsync("test-slug")).ReturnsAsync(agent);

        // Act
        var result = await _sut.GetBySlugAsync("test-slug");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-slug", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldReturnNull_WhenSlugNotFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetBySlugAsync("nonexistent")).ReturnsAsync((Agent?)null);

        // Act
        var result = await _sut.GetBySlugAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldGenerateSlugFromName()
    {
        // Arrange
        var info = new AgentInsertInfo
        {
            Name = "My Test Agent",
            SystemPrompt = "You are a test agent",
            CollectName = true
        };
        _repositoryMock.Setup(r => r.SlugExistsAsync("my-test-agent", null)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        // Act
        var result = await _sut.CreateAsync(info);

        // Assert
        Assert.Equal("My Test Agent", result.Name);
        Assert.Equal("my-test-agent", result.Slug);
        Assert.Equal(1, result.Status);
        Assert.True(result.CollectName);
    }

    [Fact]
    public async Task CreateAsync_ShouldAppendSuffix_WhenSlugExists()
    {
        // Arrange
        var info = new AgentInsertInfo { Name = "Duplicate", SystemPrompt = "P" };
        _repositoryMock.Setup(r => r.SlugExistsAsync("duplicate", null)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.SlugExistsAsync("duplicate-2", null)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        // Act
        var result = await _sut.CreateAsync(info);

        // Assert
        Assert.Equal("duplicate-2", result.Slug);
    }

    [Fact]
    public async Task CreateAsync_ShouldHandleAccentsInName()
    {
        // Arrange
        var info = new AgentInsertInfo { Name = "Agente de Atenção", SystemPrompt = "P" };
        _repositoryMock.Setup(r => r.SlugExistsAsync("agente-de-atencao", null)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        // Act
        var result = await _sut.CreateAsync(info);

        // Assert
        Assert.Equal("agente-de-atencao", result.Slug);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenAgentNotFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Agent?)null);

        // Act
        var result = await _sut.UpdateAsync(999, new AgentInsertInfo { Name = "X" });

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRegenerateSlug_WhenNameChanges()
    {
        // Arrange
        var existing = new Agent { AgentId = 1, Name = "Old Name", Slug = "old-name" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.SlugExistsAsync("new-name", 1L)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        var info = new AgentInsertInfo { Name = "New Name", SystemPrompt = "P" };

        // Act
        var result = await _sut.UpdateAsync(1, info);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result!.Name);
        Assert.Equal("new-name", result.Slug);
    }

    [Fact]
    public async Task UpdateAsync_ShouldKeepSlug_WhenNameUnchanged()
    {
        // Arrange
        var existing = new Agent { AgentId = 1, Name = "Same", Slug = "same" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>()))
            .ReturnsAsync((Agent a) => a);

        var info = new AgentInsertInfo { Name = "Same", SystemPrompt = "Updated prompt" };

        // Act
        var result = await _sut.UpdateAsync(1, info);

        // Assert
        Assert.Equal("same", result!.Slug);
        _repositoryMock.Verify(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<long?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenAgentNotFound()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Agent?)null);
        Assert.False(await _sut.DeleteAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenAgentExists()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Agent { AgentId = 1 });
        Assert.True(await _sut.DeleteAsync(1));
        _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task ToggleStatusAsync_ShouldReturnNull_WhenAgentNotFound()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Agent?)null);
        Assert.Null(await _sut.ToggleStatusAsync(999));
    }

    [Fact]
    public async Task ToggleStatusAsync_ShouldToggleFromActiveToInactive()
    {
        var agent = new Agent { AgentId = 1, Status = 1 };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(agent);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _sut.ToggleStatusAsync(1);
        Assert.Equal(0, result!.Status);
    }

    [Fact]
    public async Task ToggleStatusAsync_ShouldToggleFromInactiveToActive()
    {
        var agent = new Agent { AgentId = 1, Status = 0 };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(agent);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _sut.ToggleStatusAsync(1);
        Assert.Equal(1, result!.Status);
    }

    // --- Slugify static method tests ---

    [Theory]
    [InlineData("My Agent", "my-agent")]
    [InlineData("Agente de Atenção", "agente-de-atencao")]
    [InlineData("  Spaces  Everywhere  ", "spaces-everywhere")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("special!@#chars$%", "specialchars")]
    [InlineData("multiple---dashes", "multiple-dashes")]
    [InlineData("café latte", "cafe-latte")]
    public void Slugify_ShouldGenerateCorrectSlug(string input, string expected)
    {
        Assert.Equal(expected, AgentService.Slugify(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    public void Slugify_ShouldReturnFallback_WhenEmptyResult(string input)
    {
        Assert.Equal("agent", AgentService.Slugify(input));
    }
}
