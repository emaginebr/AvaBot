using System.Text.Json;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AvaBot.Tests.Application.Services;

public class PowerBIServiceTest
{
    private const long AgentId = 1;
    private const string Slug = "agente-teste";
    private const string SchemaJson = """
        {"tables":[{"name":"Exportacoes","columns":[{"name":"Data","dataType":"DateTime"}],"measures":[]}]}
        """;

    private readonly Mock<IAgentRepository<Agent>> _agentRepoMock = new();
    private readonly Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>> _configRepoMock = new();
    private readonly Mock<IPowerBIDatasetRepository<PowerBIDataset>> _datasetRepoMock = new();
    private readonly Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>> _queryLogRepoMock = new();
    private readonly Mock<IPowerBIClient> _clientMock = new();
    private readonly Mock<ISecretProtector> _protectorMock = new();
    private readonly PowerBIService _sut;

    public PowerBIServiceTest()
    {
        // O builder e real: o comportamento dele e controlad pela mockagem do IPowerBIClient.
        var schemaBuilder = new PowerBISchemaBuilder(_clientMock.Object);

        _sut = new PowerBIService(
            _agentRepoMock.Object,
            _configRepoMock.Object,
            _datasetRepoMock.Object,
            _queryLogRepoMock.Object,
            _clientMock.Object,
            _protectorMock.Object,
            schemaBuilder,
            NullLogger<PowerBIService>.Instance);

        _agentRepoMock.Setup(r => r.GetBySlugAsync(Slug))
            .ReturnsAsync(new Agent { AgentId = AgentId, Slug = Slug, Name = "Agente" });

        _protectorMock.Setup(p => p.Protect(It.IsAny<string>())).Returns((string plain) => $"cipher:{plain}");
        _protectorMock.Setup(p => p.Unprotect(It.IsAny<string>())).Returns("plain-secret");
    }

    private void ArrangeConfig(AgentPowerBIConfig? config)
    {
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync(config);
    }

    private static AgentPowerBIConfig ConfigWithSecret() => new()
    {
        AgentPowerBIConfigId = 5,
        AgentId = AgentId,
        TenantId = "11111111-1111-1111-1111-111111111111",
        ClientId = "22222222-2222-2222-2222-222222222222",
        ClientSecretEncrypted = "cipher:old",
        ClientSecretHint = "old"
    };

    private static PowerBIDataset UsableDataset(long id = 10) => new()
    {
        PowerBIDatasetId = id,
        AgentId = AgentId,
        Name = "Comércio Internacional",
        ToolKey = "comercio_internacional",
        WorkspaceId = "33333333-3333-3333-3333-333333333333",
        DatasetId = "44444444-4444-4444-4444-444444444444",
        SchemaJson = SchemaJson,
        SchemaStatus = PowerBISchemaStatus.Generated
    };

    [Fact]
    public async Task SaveConfigAsync_ShouldCreateTheSecretOnFirstConfig()
    {
        // Arrange
        ArrangeConfig(null);
        AgentPowerBIConfig? saved = null;
        _configRepoMock.Setup(r => r.UpsertAsync(It.IsAny<AgentPowerBIConfig>()))
            .Callback<AgentPowerBIConfig>(c => saved = c)
            .ReturnsAsync((AgentPowerBIConfig c) => c);

        // Act
        await _sut.SaveConfigAsync(Slug, new PowerBIConfigUpdateInfo
        {
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222",
            ClientSecret = "meu-segredo"
        });

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("cipher:meu-segredo", saved!.ClientSecretEncrypted);
        Assert.Equal("redo", saved.ClientSecretHint);
        _protectorMock.Verify(p => p.Protect("meu-segredo"), Times.Once);
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldThrowOnCreate_WhenTheSecretIsMissing()
    {
        // Arrange
        ArrangeConfig(null);

        // Act & Assert (FR-003: criacao exige segredo)
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SaveConfigAsync(Slug,
            new PowerBIConfigUpdateInfo { TenantId = "t", ClientId = "c", ClientSecret = null }));
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldKeepTheStoredSecret_WhenTheUpdateSendsNoSecret()
    {
        // Arrange
        ArrangeConfig(ConfigWithSecret());
        AgentPowerBIConfig? saved = null;
        _configRepoMock.Setup(r => r.UpsertAsync(It.IsAny<AgentPowerBIConfig>()))
            .Callback<AgentPowerBIConfig>(c => saved = c)
            .ReturnsAsync((AgentPowerBIConfig c) => c);

        // Act (FR-003)
        await _sut.SaveConfigAsync(Slug, new PowerBIConfigUpdateInfo
        {
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222",
            ClientSecret = "  "
        });

        // Assert
        Assert.Equal("cipher:old", saved!.ClientSecretEncrypted);
        Assert.Equal("old", saved.ClientSecretHint);
        _protectorMock.Verify(p => p.Protect(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveConfigAsync_ShouldClearTheLastTest_WhenTheCredentialsChange()
    {
        // Arrange
        var existing = ConfigWithSecret();
        existing.LastTestAt = DateTime.UtcNow;
        existing.LastTestSuccess = false;
        existing.LastTestMessage = "erro anterior";
        ArrangeConfig(existing);
        _configRepoMock.Setup(r => r.UpsertAsync(It.IsAny<AgentPowerBIConfig>()))
            .ReturnsAsync((AgentPowerBIConfig c) => c);

        // Act
        var result = await _sut.SaveConfigAsync(Slug, new PowerBIConfigUpdateInfo
        {
            TenantId = "11111111-1111-1111-1111-111111111111",
            ClientId = "22222222-2222-2222-2222-222222222222",
            ClientSecret = "novo-segredo"
        });

        // Assert
        Assert.Null(result.LastTestAt);
        Assert.Null(result.LastTestSuccess);
        Assert.Null(result.LastTestMessage);
        _clientMock.Verify(c => c.InvalidateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetConfigAsync_ShouldNeverReturnTheSecret()
    {
        // Arrange
        ArrangeConfig(ConfigWithSecret());

        // Act
        var info = await _sut.GetConfigAsync(Slug);
        var json = JsonSerializer.Serialize(info);

        // Assert (FR-005 / SC-007)
        Assert.DoesNotContain("cipher:old", json);
        Assert.Equal("••••old", info.ClientSecretMasked);
        Assert.True(info.HasClientSecret);
        Assert.True(info.IsConfigured);
    }

    [Fact]
    public async Task GetConfigAsync_ShouldReportNotConfigured_WhenTheAgentHasNoConfig()
    {
        // Arrange
        ArrangeConfig(null);

        // Act
        var info = await _sut.GetConfigAsync(Slug);

        // Assert
        Assert.False(info.IsConfigured);
        Assert.False(info.HasClientSecret);
        Assert.Null(info.TenantId);
        Assert.Equal(AgentId, info.AgentId);
    }

    [Fact]
    public async Task GetConfigAsync_ShouldThrowNotFound_ForAnUnknownAgent()
    {
        // Arrange
        _agentRepoMock.Setup(r => r.GetBySlugAsync("ninguem")).ReturnsAsync((Agent?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetConfigAsync("ninguem"));
    }

    [Fact]
    public async Task SetEnabledAsync_ShouldBlockWithoutSchema_AndExplainWhatIsMissing()
    {
        // Arrange (FR-006)
        ArrangeConfig(ConfigWithSecret());
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new List<PowerBIDataset> { new() { AgentId = AgentId, Name = "Sem schema", ToolKey = "sem_schema" } });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetEnabledAsync(Slug, true));
        Assert.Contains("schema", exception.Message);
    }

    [Fact]
    public async Task SetEnabledAsync_ShouldBlockWithoutCredentials()
    {
        // Arrange (FR-006)
        ArrangeConfig(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SetEnabledAsync(Slug, true));
        Assert.Contains("credenciais", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetEnabledAsync_ShouldTurnOn_WhenConfigAndSchemaExist()
    {
        // Arrange
        var agent = new Agent { AgentId = AgentId, Slug = Slug };
        _agentRepoMock.Setup(r => r.GetBySlugAsync(Slug)).ReturnsAsync(agent);
        ArrangeConfig(ConfigWithSecret());
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new List<PowerBIDataset> { UsableDataset() });
        _agentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        // Act
        var result = await _sut.SetEnabledAsync(Slug, true);

        // Assert
        Assert.True(agent.PowerBIEnabled);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async Task DeleteDatasetAsync_ShouldTurnTheFlagOff_WhenItRemovesTheLastUsableDataset()
    {
        // Arrange
        var agent = new Agent { AgentId = AgentId, Slug = Slug, PowerBIEnabled = true };
        _agentRepoMock.Setup(r => r.GetBySlugAsync(Slug)).ReturnsAsync(agent);
        _agentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        var dataset = UsableDataset();
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, dataset.PowerBIDatasetId)).ReturnsAsync(dataset);

        // Depois de remover, o agente fica sem dataset utilizavel
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync(new List<PowerBIDataset>());

        // Act
        var message = await _sut.DeleteDatasetAsync(Slug, dataset.PowerBIDatasetId);

        // Assert (FR-015)
        Assert.False(agent.PowerBIEnabled);
        _agentRepoMock.Verify(r => r.UpdateAsync(agent), Times.Once);
        Assert.Contains("desativado", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteDatasetAsync_ShouldKeepTheFlagOn_WhenAnotherUsableDatasetRemains()
    {
        // Arrange
        var agent = new Agent { AgentId = AgentId, Slug = Slug, PowerBIEnabled = true };
        _agentRepoMock.Setup(r => r.GetBySlugAsync(Slug)).ReturnsAsync(agent);

        var dataset = UsableDataset(10);
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, 10)).ReturnsAsync(dataset);
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new List<PowerBIDataset> { UsableDataset(11) });

        // Act
        var message = await _sut.DeleteDatasetAsync(Slug, 10);

        // Assert
        Assert.True(agent.PowerBIEnabled);
        _agentRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Agent>()), Times.Never);
        Assert.DoesNotContain("desativado", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDatasetAsync_ShouldRejectADuplicateDatasetForTheAgent()
    {
        // Arrange
        _datasetRepoMock.Setup(r => r.ExistsAsync(AgentId, "44444444-4444-4444-4444-444444444444", null))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateDatasetAsync(Slug,
            new PowerBIDatasetInsertInfo
            {
                WorkspaceId = "33333333-3333-3333-3333-333333333333",
                DatasetId = "44444444-4444-4444-4444-444444444444",
                Name = "Comércio Internacional"
            }));
    }

    [Fact]
    public async Task CreateDatasetAsync_ShouldGenerateAnUnderscoredToolKey()
    {
        // Arrange
        PowerBIDataset? created = null;
        _datasetRepoMock.Setup(r => r.CreateAsync(It.IsAny<PowerBIDataset>()))
            .Callback<PowerBIDataset>(d => created = d)
            .ReturnsAsync((PowerBIDataset d) => d);

        // Act
        var result = await _sut.CreateDatasetAsync(Slug, new PowerBIDatasetInsertInfo
        {
            WorkspaceId = "33333333-3333-3333-3333-333333333333",
            DatasetId = "44444444-4444-4444-4444-444444444444",
            Name = "Comércio Internacional",
            Description = "Exportações de pescado"
        });

        // Assert (FR-017)
        Assert.Equal("comercio_internacional", created!.ToolKey);
        Assert.Equal("comercio_internacional", result.ToolKey);
        Assert.Equal(PowerBISchemaStatus.NotGenerated, (PowerBISchemaStatus)result.SchemaStatus);
    }

    [Fact]
    public async Task UpdateDatasetAsync_ShouldResetTheSchema_WhenTheBindingChanges()
    {
        // Arrange
        var dataset = UsableDataset();
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, dataset.PowerBIDatasetId)).ReturnsAsync(dataset);
        _datasetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PowerBIDataset>())).ReturnsAsync((PowerBIDataset d) => d);

        // Act
        var result = await _sut.UpdateDatasetAsync(Slug, dataset.PowerBIDatasetId, new PowerBIDatasetInsertInfo
        {
            WorkspaceId = dataset.WorkspaceId,
            DatasetId = "55555555-5555-5555-5555-555555555555",
            Name = dataset.Name
        });

        // Assert (FR-009)
        Assert.Null(result.SchemaGeneratedAt);
        Assert.Equal(PowerBISchemaStatus.NotGenerated, (PowerBISchemaStatus)dataset.SchemaStatus);
        Assert.Null(dataset.SchemaJson);
    }

    [Fact]
    public async Task GenerateSchemaAsync_ShouldPreserveThePreviousSchema_WhenGenerationFails()
    {
        // Arrange (FR-010): consulta recusada com PowerBIEntityNotFound
        var dataset = UsableDataset();
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, dataset.PowerBIDatasetId)).ReturnsAsync(dataset);
        ArrangeConfig(ConfigWithSecret());
        _datasetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PowerBIDataset>())).ReturnsAsync((PowerBIDataset d) => d);

        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(q => q.Contains("INFO.VIEW")), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(404, "PowerBIEntityNotFound", "no query permission"));

        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(q => q.Contains("COLUMNSTATISTICS")), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(404, "PowerBIEntityNotFound", "no query permission"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GenerateSchemaAsync(Slug, dataset.PowerBIDatasetId));

        Assert.Equal(PowerBISchemaStatus.Error, dataset.SchemaStatus);
        Assert.NotNull(dataset.SchemaError);
        Assert.Equal(SchemaJson, dataset.SchemaJson);
    }

    [Fact]
    public async Task GenerateSchemaAsync_ShouldMergeTheUserDescriptions()
    {
        // Arrange
        var dataset = UsableDataset();
        dataset.SchemaJson = """
            {"tables":[{"name":"Exportacoes","userDescription":"descrição do admin","columns":[{"name":"Data","dataType":"DateTime","userDescription":"use por periodo"}],"measures":[]}]}
            """;
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, dataset.PowerBIDatasetId)).ReturnsAsync(dataset);
        ArrangeConfig(ConfigWithSecret());
        _datasetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PowerBIDataset>())).ReturnsAsync((PowerBIDataset d) => d);

        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(q => q.EndsWith("TABLES()")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult
            {
                Columns = new List<string> { "Table[Name]", "Table[Description]", "Table[IsHidden]" },
                Rows = new List<List<object?>> { new() { "Exportacoes", null, false } }
            });

        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(q => q.EndsWith("COLUMNS()")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult
            {
                Columns = new List<string> { "Column[Table]", "Column[Name]", "Column[DataType]", "Column[Description]", "Column[IsHidden]" },
                Rows = new List<List<object?>> { new() { "Exportacoes", "Data", "DateTime", null, false } }
            });

        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(q => q.EndsWith("MEASURES()")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult
            {
                Columns = new List<string> { "Measure[Table]", "Measure[Name]", "Measure[Description]", "Measure[IsHidden]" },
                Rows = new List<List<object?>>()
            });

        // Enriquecimento (relacionamentos e valores de exemplo) sem dados
        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(q => q.EndsWith("RELATIONSHIPS()") || q.EndsWith("COLUMNSTATISTICS()")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult());

        // Act
        var result = await _sut.GenerateSchemaAsync(Slug, dataset.PowerBIDatasetId);

        // Assert (FR-012)
        Assert.Equal(PowerBISchemaStatus.Generated, (PowerBISchemaStatus)result.SchemaStatus);
        Assert.NotNull(result.SchemaGeneratedAt);
        Assert.Null(result.SchemaError);
        Assert.Equal("descrição do admin", result.Tables.Single().UserDescription);
        Assert.Equal("use por periodo", result.Tables.Single().Columns.Single().UserDescription);
    }

    [Fact]
    public async Task UpdateSchemaDescriptionsAsync_ShouldRejectAnItemThatDoesNotExist()
    {
        // Arrange
        var dataset = UsableDataset();
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, dataset.PowerBIDatasetId)).ReturnsAsync(dataset);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateSchemaDescriptionsAsync(Slug, dataset.PowerBIDatasetId,
            new PowerBISchemaDescriptionUpdateInfo
            {
                Items = new List<PowerBISchemaDescriptionItemInfo>
                {
                    new() { Kind = "column", Table = "Exportacoes", Name = "ColunaQueNaoExiste", UserDescription = "x" }
                }
            }));
    }

    [Fact]
    public async Task UpdateSchemaDescriptionsAsync_ShouldClearTheDescriptionWhenItIsBlank()
    {
        // Arrange
        var dataset = UsableDataset();
        dataset.SchemaJson = """
            {"tables":[{"name":"Exportacoes","userDescription":"admin","columns":[],"measures":[]}]}
            """;
        _datasetRepoMock.Setup(r => r.GetByIdAsync(AgentId, dataset.PowerBIDatasetId)).ReturnsAsync(dataset);
        _datasetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<PowerBIDataset>())).ReturnsAsync((PowerBIDataset d) => d);

        // Act
        var result = await _sut.UpdateSchemaDescriptionsAsync(Slug, dataset.PowerBIDatasetId,
            new PowerBISchemaDescriptionUpdateInfo
            {
                Items = new List<PowerBISchemaDescriptionItemInfo>
                {
                    new() { Kind = "table", Table = "exportacoes", Name = null, UserDescription = "   " }
                }
            });

        // Assert
        Assert.Null(result.Tables.Single().UserDescription);
    }

    [Fact]
    public async Task GetQueryLogsAsync_ShouldLimitThePageSizeToOneHundred()
    {
        // Arrange
        _queryLogRepoMock.Setup(r => r.GetPagedByAgentAsync(AgentId, 1, 100))
            .ReturnsAsync((new List<PowerBIQueryLog>(), 0));

        // Act
        var result = await _sut.GetQueryLogsAsync(Slug, 1, 5000);

        // Assert
        Assert.Equal(100, result.PageSize);
        _queryLogRepoMock.Verify(r => r.GetPagedByAgentAsync(AgentId, 1, 100), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldGuideTheAdmin_WhenTheDatasetIsNotQueryable()
    {
        // Arrange (research R3: Viewer recebe PowerBIEntityNotFound, nao 403)
        ArrangeConfig(ConfigWithSecret());
        _configRepoMock.Setup(r => r.UpsertAsync(It.IsAny<AgentPowerBIConfig>()))
            .ReturnsAsync((AgentPowerBIConfig c) => c);

        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new List<PowerBIDataset> { UsableDataset() });

        _clientMock.Setup(c => c.ListWorkspacesWithDatasetsAsync(It.IsAny<PowerBICredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PowerBIWorkspace> { new() { Id = "ws", Name = "BI ABIPESCA" } });

        _clientMock.Setup(c => c.ExecuteQueryAsync(
                It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(404, "PowerBIEntityNotFound", "no query permission"));

        // Act
        var result = await _sut.TestConnectionAsync(Slug);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.Steps[0].Success);
        Assert.True(result.Steps[1].Success);
        Assert.False(result.Steps[2].Success);
        Assert.Contains("Contributor", result.Steps[2].Message);

        Assert.Equal(1, result.Steps.Count(s => s.Step == "auth"));
        Assert.StartsWith("dataset:", result.Steps[2].Step);
    }
}
