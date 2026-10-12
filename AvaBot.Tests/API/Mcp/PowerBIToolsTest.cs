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
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.API.Mcp;

public class PowerBIToolsTest
{
    private const long OwnerId = McpTestContext.UserId;
    private const long AgentId = 10;
    private const string Slug = "vendas";
    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>> _configRepoMock = new();
    private readonly Mock<IPowerBIDatasetRepository<PowerBIDataset>> _datasetRepoMock = new();
    private readonly Mock<IPowerBIClient> _clientMock = new();
    private readonly McpCaller _caller = new(OwnerId, McpTestContext.KeyId, "Claude");
    private readonly Agent _agent = new() { AgentId = AgentId, OwnerUserId = OwnerId, Slug = Slug, Name = "Vendas", PowerBIEnabled = false };
    private readonly PowerBITools _sut;

    public PowerBIToolsTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var protectorMock = new Mock<ISecretProtector>();
        protectorMock.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("plain-secret");

        var agentService = new AgentService(_agentRepoMock.Object, new Mock<IElasticsearchService>().Object, protectorMock.Object, mapper);
        var powerBIService = new PowerBIService(_agentRepoMock.Object, _configRepoMock.Object, _datasetRepoMock.Object,
            new Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>>().Object, _clientMock.Object, protectorMock.Object,
            new PowerBISchemaBuilder(_clientMock.Object), NullLogger<PowerBIService>.Instance);
        IValidator<PowerBIDatasetInsertInfo> validator = new PowerBIDatasetInsertInfoValidator();

        _sut = new PowerBITools(agentService, powerBIService, validator, NullLogger<PowerBITools>.Instance);

        _agentRepoMock.Setup(r => r.GetByIdAsync(AgentId, OwnerId)).ReturnsAsync(_agent);
        _agentRepoMock.Setup(r => r.GetBySlugAsync(Slug, OwnerId)).ReturnsAsync(_agent);
        _agentRepoMock.Setup(r => r.GetByIdAsync(99, OwnerId)).ReturnsAsync((Agent?)null);
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync(new List<PowerBIDataset>());
    }

    private static AgentPowerBIConfig ConfigWithSecret() => new()
    {
        AgentPowerBIConfigId = 5,
        AgentId = AgentId,
        TenantId = "11111111-1111-1111-1111-111111111111",
        ClientId = "22222222-2222-2222-2222-222222222222",
        ClientSecretEncrypted = "cipher:segredo",
        ClientSecretHint = "redo",
        LastTestAt = DateTime.UtcNow,
        LastTestSuccess = true,
        LastTestMessage = "ok"
    };

    [Fact]
    public async Task ObterPowerBI_ShouldReturnIndicatorsOnly_WithoutTenantClientOrSecret()
    {
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync(ConfigWithSecret());

        var result = await _sut.ObterPowerBIAsync(_caller, AgentId);

        Assert.NotEqual(true, result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"credentialConfigured\":true", text);
        Assert.Contains("\"lastTestSuccess\":true", text);
        Assert.DoesNotContain("11111111", text);
        Assert.DoesNotContain("22222222", text);
        Assert.DoesNotContain("redo", text);
        Assert.DoesNotContain("tenantId", text);
        Assert.DoesNotContain("clientId", text);
        Assert.DoesNotContain("clientSecret", text);
    }

    [Fact]
    public async Task ObterPowerBI_ShouldSayCredentialNotConfigured_WhenThereIsNoConfig()
    {
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync((AgentPowerBIConfig?)null);

        var result = await _sut.ObterPowerBIAsync(_caller, AgentId);

        Assert.Contains("\"credentialConfigured\":false", McpTestContext.Text(result));
    }

    [Fact]
    public async Task DefinirPowerBIAtivo_ShouldGuideTheUser_WhenEnablingWithoutCredential()
    {
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync((AgentPowerBIConfig?)null);

        var result = await _sut.DefinirPowerBIAtivoAsync(_caller, AgentId, true);

        Assert.True(result.IsError);
        Assert.Equal(PowerBITools.MissingCredentialMessage("Vendas"), McpTestContext.Text(result));
        _agentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task ListarWorkspacesPowerBI_ShouldNotCallMicrosoft_WithoutCredential()
    {
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync((AgentPowerBIConfig?)null);

        var result = await _sut.ListarWorkspacesPowerBIAsync(_caller, AgentId);

        Assert.True(result.IsError);
        Assert.Contains("ainda nao tem credencial", McpTestContext.Text(result));
        _clientMock.Verify(c => c.ListWorkspacesWithDatasetsAsync(It.IsAny<PowerBICredentials>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarDatasetPowerBI_ShouldPreserveWorkspaceAndDatasetIds()
    {
        var dataset = new PowerBIDataset
        {
            PowerBIDatasetId = 7, AgentId = AgentId, WorkspaceId = "ws-1", DatasetId = "ds-1",
            Name = "Faturamento", Description = "antiga", ToolKey = "faturamento", SchemaStatus = PowerBISchemaStatus.NotGenerated
        };
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync(new List<PowerBIDataset> { dataset });
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, 7)).ReturnsAsync(dataset);
        _datasetRepoMock.Setup(r => r.ToolKeyExistsAsync(AgentId, It.IsAny<string>(), 7)).ReturnsAsync(false);
        _datasetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PowerBIDataset>())).ReturnsAsync((PowerBIDataset d) => d);

        var result = await _sut.AtualizarDatasetPowerBIAsync(_caller, AgentId, 7, null, "nova descricao");

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("ws-1", dataset.WorkspaceId);
        Assert.Equal("ds-1", dataset.DatasetId);
        Assert.Equal("Faturamento", dataset.Name);
        Assert.Equal("nova descricao", dataset.Description);
    }

    [Fact]
    public async Task ObterPowerBI_ShouldAnswerNotFound_ForAForeignAgent()
    {
        var result = await _sut.ObterPowerBIAsync(_caller, 99);

        Assert.True(result.IsError);
        Assert.Equal("Agente nao encontrado", McpTestContext.Text(result));
    }
}
