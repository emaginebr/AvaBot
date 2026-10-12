using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Mcp;
using AvaBot.API.Mcp.Tools;
using AvaBot.API.Validators;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Mcp;

public class AgentToolsTest
{
    private const long OwnerId = McpTestContext.UserId;
    private readonly Mock<IAgentRepository<Agent>> _repoMock = new();
    private readonly McpCaller _caller = new(OwnerId, McpTestContext.KeyId, "Claude");
    private readonly AgentTools _sut;

    public AgentToolsTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        expr.AddProfile<McpProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var agentService = new AgentService(_repoMock.Object, new Mock<IElasticsearchService>().Object, new Mock<ISecretProtector>().Object, mapper);
        IValidator<AgentInsertInfo> validator = new AgentInsertInfoValidator();

        _sut = new AgentTools(agentService, validator, mapper, NullLogger<AgentTools>.Instance);
    }

    private static Agent AgentWithSecrets(long id = 12) => new()
    {
        AgentId = id,
        OwnerUserId = OwnerId,
        Name = "Suporte Loja",
        Slug = "suporte-loja",
        SystemPrompt = "prompt",
        ChatModel = "gpt-4o",
        Status = 1,
        OpenAIApiKeyEncrypted = "ENC-sk-live",
        TelegramBotToken = "123:TOKEN",
        WhatsappToken = "WPP"
    };

    [Fact]
    public async Task CriarAgente_ShouldNeverFillSecretFields_AndCreateTheAgent()
    {
        Agent? created = null;
        _repoMock.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<long?>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Agent>()))
            .Callback<Agent>(a => created = a)
            .ReturnsAsync((Agent a) => { a.AgentId = 20; return a; });

        var result = await _sut.CriarAgenteAsync(_caller, new AgentMcpCreateInfo { Name = "Suporte Loja", SystemPrompt = "Responda so sobre trocas", CollectEmail = true });

        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(created);
        Assert.Null(created!.OpenAIApiKeyEncrypted);
        Assert.Null(created.TelegramBotToken);
        Assert.Null(created.TelegramWebhookSecret);
        Assert.Equal(OwnerId, created.OwnerUserId);
        Assert.Equal("gpt-4o", created.ChatModel);
        Assert.True(created.CollectEmail);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"agentId\":20", text);
        Assert.Contains("\"hasOpenAIApiKey\":false", text);
    }

    [Fact]
    public async Task CriarAgente_ShouldReturnTheValidatorMessages_WhenTheNameIsEmpty()
    {
        var result = await _sut.CriarAgenteAsync(_caller, new AgentMcpCreateInfo { Name = "", SystemPrompt = "" });

        Assert.True(result.IsError);
        Assert.Equal("Nome e obrigatorio; Prompt de sistema e obrigatorio", McpTestContext.Text(result));
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task ObterAgente_ShouldReturnNotFound_ForAnUnknownOrForeignId()
    {
        _repoMock.Setup(r => r.GetByIdAsync(999, OwnerId)).ReturnsAsync((Agent?)null);

        var result = await _sut.ObterAgenteAsync(_caller, 999);

        Assert.True(result.IsError);
        Assert.Equal("Agente nao encontrado", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ObterAgente_ShouldReturnIndicatorsOnly_NeverTokens()
    {
        _repoMock.Setup(r => r.GetByIdAsync(12, OwnerId)).ReturnsAsync(AgentWithSecrets());

        var result = await _sut.ObterAgenteAsync(_caller, 12);

        Assert.NotEqual(true, result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"hasOpenAIApiKey\":true", text);
        Assert.Contains("\"telegramBotConfigured\":true", text);
        Assert.Contains("\"whatsappConfigured\":true", text);
        Assert.DoesNotContain("ENC-sk-live", text);
        Assert.DoesNotContain("123:TOKEN", text);
        Assert.DoesNotContain("WPP", text);
        Assert.DoesNotContain("telegramBotToken", text);
        Assert.NotNull(result.StructuredContent);
    }

    [Fact]
    public async Task ListarAgentes_ShouldMapActiveFromStatus()
    {
        _repoMock.Setup(r => r.GetAllByOwnerAsync(OwnerId)).ReturnsAsync(
        [
            AgentWithSecrets(1),
            new Agent { AgentId = 2, Name = "Inativo", Slug = "inativo", Status = 0, ChatModel = "gpt-4o" }
        ]);

        var result = await _sut.ListarAgentesAsync(_caller);

        var text = McpTestContext.Text(result);
        Assert.Contains("\"agentId\":1,\"name\":\"Suporte Loja\"", text);
        Assert.Contains("\"active\":true", text);
        Assert.Contains("\"active\":false", text);
        Assert.DoesNotContain("systemPrompt", text);
    }

    [Fact]
    public async Task AtualizarAgente_ShouldUpdatePartially_AndKeepSecrets()
    {
        var agent = AgentWithSecrets();
        _repoMock.Setup(r => r.GetByIdAsync(12, OwnerId)).ReturnsAsync(agent);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _sut.AtualizarAgenteAsync(_caller, 12, new AgentProfileUpdateInfo { SystemPrompt = "mais formal" });

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("mais formal", agent.SystemPrompt);
        Assert.Equal("ENC-sk-live", agent.OpenAIApiKeyEncrypted);
        Assert.Equal("123:TOKEN", agent.TelegramBotToken);
    }

    [Fact]
    public async Task DefinirAgenteAtivo_ShouldReturnTheAgentWithActiveFalse()
    {
        var agent = AgentWithSecrets();
        _repoMock.Setup(r => r.GetByIdAsync(12, OwnerId)).ReturnsAsync(agent);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var result = await _sut.DefinirAgenteAtivoAsync(_caller, 12, false);

        Assert.Contains("\"active\":false", McpTestContext.Text(result));
        Assert.Equal(0, agent.Status);
    }

    [Fact]
    public void AgentTools_ShouldNotExposeAnyDeleteTool()
    {
        var toolNames = typeof(AgentTools).GetMethods()
            .Select(m => m.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false)
                .OfType<ModelContextProtocol.Server.McpServerToolAttribute>().FirstOrDefault()?.Name)
            .Where(n => n != null)
            .ToList();

        Assert.Equal(5, toolNames.Count);
        Assert.DoesNotContain(toolNames, n => n!.Contains("excluir") || n.Contains("remover") || n.Contains("deletar") || n.Contains("delete"));
    }
}
