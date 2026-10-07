using System.Text.Json;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AvaBot.Tests.Application.Services;

public class PowerBIToolProviderTest
{
    private const long AgentId = 1;

    private readonly Mock<IAgentPowerBIConfigRepository<AgentPowerBIConfig>> _configRepoMock = new();
    private readonly Mock<IPowerBIDatasetRepository<PowerBIDataset>> _datasetRepoMock = new();
    private readonly Mock<IPowerBIQueryLogRepository<PowerBIQueryLog>> _queryLogRepoMock = new();
    private readonly Mock<IPowerBIClient> _clientMock = new();
    private readonly Mock<ISecretProtector> _protectorMock = new();
    private readonly PowerBIToolProvider _sut;

    public PowerBIToolProviderTest()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PowerBI:MaxRows"] = "2",
                ["PowerBI:QueryTimeoutSeconds"] = "30"
            })
            .Build();

        _sut = new PowerBIToolProvider(
            _configRepoMock.Object,
            _datasetRepoMock.Object,
            _queryLogRepoMock.Object,
            _clientMock.Object,
            _protectorMock.Object,
            configuration,
            NullLogger<PowerBIToolProvider>.Instance);

        ArrangeCredentials();
    }

    private void ArrangeCredentials()
    {
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(new AgentPowerBIConfig
            {
                AgentId = AgentId,
                TenantId = "tenant",
                ClientId = "client",
                ClientSecretEncrypted = "cipher"
            });

        _protectorMock.Setup(p => p.Unprotect("cipher")).Returns("secret");
    }

    private static PowerBIDataset Dataset(string toolKey = "comercio", string? schemaJson = null) => new()
    {
        PowerBIDatasetId = 10,
        AgentId = AgentId,
        Name = "Comércio Internacional",
        ToolKey = toolKey,
        WorkspaceId = "workspace",
        DatasetId = "dataset",
        SchemaJson = schemaJson ?? PowerBISchema.Serialize(new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new()
                {
                    Name = "Exportacoes",
                    UserDescription = "exportações de pescado",
                    Columns = new List<PowerBISchemaColumn>
                    {
                        new() { Name = "Data", DataType = "DateTime", UserDescription = "use para período" }
                    },
                    Measures = new List<PowerBISchemaMeasure>
                    {
                        new() { Name = "Valor FOB (US$)", UserDescription = "valor em dólares" }
                    }
                }
            }
        })
    };

    private static PowerBIDataset DatasetWithoutSchema(string toolKey = "comercio") => new()
    {
        PowerBIDatasetId = 11,
        AgentId = AgentId,
        Name = "Sem Schema",
        ToolKey = toolKey,
        WorkspaceId = "workspace",
        DatasetId = "dataset-2",
        SchemaJson = null,
        SchemaStatus = PowerBISchemaStatus.NotGenerated
    };

    private void ArrangeDatasets(params PowerBIDataset[] datasets)
    {
        _datasetRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId))
            .ReturnsAsync(datasets.ToList());
    }

    private static Agent EnabledAgent() => new() { AgentId = AgentId, PowerBIEnabled = true };

    private static ChatToolCall Call(string name, string argumentsJson) => new()
    {
        Id = "call-1",
        Name = name,
        ArgumentsJson = argumentsJson
    };

    private async Task<PowerBIToolset> BuildToolset(params PowerBIDataset[] datasets)
    {
        ArrangeDatasets(datasets);
        var toolset = await _sut.GetToolsetAsync(EnabledAgent(), null, "pergunta");
        return toolset!;
    }

    [Fact]
    public async Task GetToolsetAsync_ShouldReturnNull_WhenTheFlagIsOff()
    {
        // Arrange
        ArrangeDatasets(Dataset());

        // Act
        var toolset = await _sut.GetToolsetAsync(new Agent { AgentId = AgentId, PowerBIEnabled = false }, null, "pergunta");

        // Assert
        Assert.Null(toolset);
    }

    [Fact]
    public async Task GetToolsetAsync_ShouldReturnNull_WhenThereIsNoConfig()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetByAgentIdAsync(AgentId)).ReturnsAsync((AgentPowerBIConfig?)null);
        ArrangeDatasets(Dataset());

        // Act
        var toolset = await _sut.GetToolsetAsync(EnabledAgent(), null, "pergunta");

        // Assert
        Assert.Null(toolset);
    }

    [Fact]
    public async Task GetToolsetAsync_ShouldReturnNull_WhenNoDatasetHasSchema()
    {
        // Arrange
        ArrangeDatasets(DatasetWithoutSchema());

        // Act
        var toolset = await _sut.GetToolsetAsync(EnabledAgent(), null, "pergunta");

        // Assert
        Assert.Null(toolset);
    }

    [Fact]
    public async Task GetToolsetAsync_ShouldExposeTwoToolsWithTheAgentDatasetKeys()
    {
        // Arrange
        ArrangeDatasets(Dataset("comercio"), Dataset("estatisticas") );

        // Act
        var toolset = await _sut.GetToolsetAsync(EnabledAgent(), null, "pergunta");

        // Assert
        Assert.NotNull(toolset);
        Assert.Equal(2, toolset.Definitions.Count);
        Assert.Equal(PowerBIToolset.ListSchemaToolName, toolset.Definitions[0].Name);
        Assert.Equal(PowerBIToolset.QueryToolName, toolset.Definitions[1].Name);
        Assert.Contains("DADOS DO POWER BI", toolset.SystemPromptAddendum);

        foreach (var definition in toolset.Definitions)
        {
            Assert.Contains("\"comercio\"", definition.ParametersJsonSchema);
            Assert.Contains("\"estatisticas\"", definition.ParametersJsonSchema);
            Assert.DoesNotContain("\"outro_agente\"", definition.ParametersJsonSchema);
        }

        // O catalogo de datasets vai na descricao da primeira tool (R8)
        Assert.Contains("comercio: Comércio Internacional", toolset.Definitions[0].Description);
    }

    [Fact]
    public async Task ExecuteAsync_ListSchema_ShouldReturnCompactTextAndLogSuccess()
    {
        // Arrange
        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.ListSchemaToolName, "{\"dataset\":\"comercio\"}"), CancellationToken.None);

        // Assert
        Assert.Contains("Dataset: Comércio Internacional", result);
        Assert.Contains("Tabela Exportacoes — exportações de pescado", result);
        Assert.Contains("Data (DateTime) — use para período", result);
        Assert.Contains("[Valor FOB (US$)] — valor em dólares", result);

        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l =>
            l.ToolName == PowerBIToolset.ListSchemaToolName
            && l.Status == PowerBIQueryStatus.Success
            && l.AgentId == AgentId
            && l.UserQuestion == "pergunta")), Times.Once);

        // listar_schema nao consulta o Power BI (contract llm-tools.md)
        _clientMock.Verify(c => c.ExecuteQueryAsync(
            It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnErrorAndLog_WhenTheDatasetIsNotFromThisAgent()
    {
        // Arrange
        var toolset = await BuildToolset(Dataset("comercio"));

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"de_outro_agente\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
        Assert.False(doc.RootElement.TryGetProperty("rows", out _));

        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l =>
            l.Status == PowerBIQueryStatus.Error && l.PowerBIDatasetId == null)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectDaxWithoutEvaluateOrDefine()
    {
        // Arrange
        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"DELETE FROM x\"}"),
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("A consulta DAX deve começar com EVALUATE ou DEFINE.", doc.RootElement.GetProperty("error").GetString());

        _clientMock.Verify(c => c.ExecuteQueryAsync(
            It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAcceptDaxThatStartsWithAComment()
    {
        // Arrange
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult
            {
                Columns = new List<string> { "Produto[Nome]" },
                Rows = new List<List<object?>> { new() { "Tilápia" } }
            });

        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName,
                "{\"dataset\":\"comercio\",\"dax\":\"// total\\nEVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
        Assert.Equal(1, doc.RootElement.GetProperty("rowCount").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTruncateRowsAtMaxRows()
    {
        // Arrange: MaxRows = 2 na configuracao deste teste
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult
            {
                Columns = new List<string> { "[Valor]" },
                Rows = new List<List<object?>>
                {
                    new() { 1 },
                    new() { 2 },
                    new() { 3 },
                    new() { 4 }
                }
            });

        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE '[Exportacoes]'\"}"),
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetProperty("rowCount").GetInt32());
        Assert.True(doc.RootElement.GetProperty("truncated").GetBoolean());
        Assert.Equal(2, doc.RootElement.GetProperty("rows").GetArrayLength());

        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l =>
            l.Truncated && l.RowCount == 2 && l.Status == PowerBIQueryStatus.Success)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPermissionGuidanceAndLogError_WhenPowerBIRefusesTheQuery()
    {
        // Arrange: PowerBIEntityNotFound e o falso negativo de permissao do modelo PBIR (research R3)
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(404, "PowerBIEntityNotFound", "The dataset 'x' does not support queries."));

        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Contains("Contributor", doc.RootElement.GetProperty("error").GetString());

        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l =>
            l.Status == PowerBIQueryStatus.Error && l.ErrorMessage != null)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRecordExecutedQueriesForTheTestPage()
    {
        // Arrange
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PowerBIQueryResult
            {
                Columns = new List<string> { "[Valor]" },
                Rows = new List<List<object?>> { new() { 12345.67 } }
            });

        var toolset = await BuildToolset(Dataset());

        // Act
        await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert (FR-028)
        var query = Assert.Single(toolset.ExecutedQueries);
        Assert.Equal(PowerBIToolset.QueryToolName, query.ToolName);
        Assert.Equal("Comércio Internacional", query.DatasetName);
        Assert.Equal("EVALUATE ROW(1,1)", query.Query);
        Assert.True(query.Success);
        Assert.Contains("12345.67", query.ResultPreview);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNeverThrowToTheCaller()
    {
        // Arrange
        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call("ferramenta_inexistente", "{}"), CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Contains("ferramenta_inexistente", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotLeakTheClientSecretInLogsOrResults()
    {
        // Arrange
        _protectorMock.Setup(p => p.Unprotect("cipher")).Returns("super-segredo");
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(500, "Error", "Falha usando super-segredo como credencial"));

        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert (SC-007)
        Assert.DoesNotContain("super-segredo", result);

        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l =>
            l.ErrorMessage != null && !l.ErrorMessage.Contains("super-segredo"))), Times.Once);
    }
}
