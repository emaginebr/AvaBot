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
    public async Task ExecuteAsync_ListSchema_ShouldRenderNamesAsDaxReferences()
    {
        // Arrange: "public ABIPESCA_COMTRADE" sem aspas levava o modelo a usar 'ABIPESCA_COMTRADE'
        var schemaJson = PowerBISchema.Serialize(new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new()
                {
                    Name = "public ABIPESCA_COMTRADE",
                    Columns = new List<PowerBISchemaColumn> { new() { Name = "Valor (US$)", DataType = "Number" } }
                },
                new()
                {
                    Name = "D'Agua",
                    Measures = new List<PowerBISchemaMeasure> { new() { Name = "Total [kg]" } }
                }
            }
        });
        var toolset = await BuildToolset(Dataset(schemaJson: schemaJson));

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.ListSchemaToolName, "{\"dataset\":\"comercio\"}"), CancellationToken.None);

        // Assert
        Assert.Contains("Tabela 'public ABIPESCA_COMTRADE'", result);
        Assert.Contains("[Valor (US$)] (Number)", result);
        Assert.Contains("Tabela 'D''Agua'", result);
        Assert.Contains("[Total [kg]]]", result);
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
        Assert.Contains("Tabela 'Exportacoes' — exportações de pescado", result);
        Assert.Contains("[Data] (DateTime) — use para período", result);
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

        // Assert: contrato de consultar_bi devolve diagnostico estruturado (014)
        using var doc = JsonDocument.Parse(result);
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal(PowerBIToolset.CategoryDaxQuery, error.GetProperty("category").GetString());
        Assert.Contains("EVALUATE ou DEFINE", error.GetProperty("message").GetString());
        Assert.True(error.GetProperty("queryMayBeCorrected").GetBoolean());

        // Rejeicao local nao enviou requisicao: nao consome orcamento nem chama o Power BI.
        Assert.Equal(0, error.GetProperty("attemptNumber").GetInt32());

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
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal(PowerBIToolset.CategoryAuthOrPermission, error.GetProperty("category").GetString());
        Assert.Contains("Contributor", error.GetProperty("message").GetString());
        Assert.False(error.GetProperty("queryMayBeCorrected").GetBoolean());

        // D2: erro de autenticacao/permissao nao e reenviado nem repetido.
        _clientMock.Verify(c => c.ExecuteQueryAsync(
            It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

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
        // Arrange: 400 (novo orcamento nao repete) com o segredo tambem no corpo da resposta
        _protectorMock.Setup(p => p.Unprotect("cipher")).Returns("super-segredo");
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(400, "DatasetExecuteQueriesError",
                "Falha usando super-segredo como credencial", "corpo: super-segredo aparecia aqui"));

        var toolset = await BuildToolset(Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert (SC-007): redacao vale para mensagem E corpo integral preservado
        Assert.DoesNotContain("super-segredo", result);
        Assert.Contains("[redacted]", result);
        Assert.Contains("corpo:", result);

        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l =>
            l.ErrorMessage != null && !l.ErrorMessage.Contains("super-segredo"))), Times.Once);
    }

    // ---------- 014: orcamento, retentativa e diagnostico integral ----------

    private PowerBIToolProvider BuildProvider(int maxQueryAttempts)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PowerBI:MaxRows"] = "2",
                ["PowerBI:QueryTimeoutSeconds"] = "30",
                ["PowerBI:MaxQueryAttempts"] = maxQueryAttempts.ToString()
            })
            .Build();

        return new PowerBIToolProvider(
            _configRepoMock.Object,
            _datasetRepoMock.Object,
            _queryLogRepoMock.Object,
            _clientMock.Object,
            _protectorMock.Object,
            configuration,
            NullLogger<PowerBIToolProvider>.Instance);
    }

    private async Task<PowerBIToolset> BuildToolsetWith(PowerBIToolProvider provider, PowerBIDataset dataset)
    {
        ArrangeDatasets(dataset);
        var toolset = await provider.GetToolsetAsync(EnabledAgent(), null, "pergunta");
        return toolset!;
    }

    private static PowerBIApiException RateLimited() =>
        new(429, "DatasetExecuteQueriesRateLimit", "Muitas requisicoes.", null, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_ShouldRepeatTheSameDax_WhenTheServiceIsTransient()
    {
        // Arrange: dois 429 (Retry-After zero) e depois sucesso
        var sent = new List<string>();
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PowerBICredentials _, string _, string _, string dax, CancellationToken _) =>
            {
                sent.Add(dax);

                if (sent.Count < 3)
                    throw RateLimited();

                return new PowerBIQueryResult
                {
                    Columns = new List<string> { "[Valor]" },
                    Rows = new List<List<object?>> { new() { 10 } }
                };
            });

        var toolset = await BuildToolsetWith(BuildProvider(5), Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert: tres execucoes da MESMA dax, sem pedir correcao ao modelo (D2)
        Assert.Equal(3, sent.Count);
        Assert.All(sent, dax => Assert.Equal("EVALUATE ROW(1,1)", dax));
        Assert.Contains("rowCount", result);
        Assert.DoesNotContain("\"error\"", result);

        // cada requisicao que falhou tem registro proprio: 2 falhas + 1 sucesso
        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l => l.Status == PowerBIQueryStatus.Error)), Times.Exactly(2));
        _queryLogRepoMock.Verify(r => r.CreateAsync(It.Is<PowerBIQueryLog>(l => l.Status == PowerBIQueryStatus.Success)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopRequestingPowerBI_WhenTheBudgetIsExhausted()
    {
        // Arrange
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(RateLimited());

        var toolset = await BuildToolsetWith(BuildProvider(2), Dataset());

        // Act: o modelo chama de novo depois de esgotar o orcamento
        var first = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);
        var second = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(2,2)\"}"),
            CancellationToken.None);

        // Assert: exatamente 2 requisicoes HTTP no total, apesar de 2 chamadas de tool
        _clientMock.Verify(c => c.ExecuteQueryAsync(
            It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        using var doc = JsonDocument.Parse(first);
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal(PowerBIToolset.CategoryAttemptLimit, error.GetProperty("category").GetString());
        Assert.False(error.GetProperty("queryMayBeCorrected").GetBoolean());
        Assert.Equal(2, error.GetProperty("maxAttempts").GetInt32());
        Assert.Equal(2, error.GetProperty("attemptNumber").GetInt32());

        // a chamada extra nao pode voltar a chamar o Power BI (D3)
        Assert.Contains(PowerBIToolset.CategoryAttemptLimit, second);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepTheFullQueryBudget_AfterReadingSchema()
    {
        // Arrange: listar_schema nao pode reduzir as execucoes de consultar_bi (D3)
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(RateLimited());

        var toolset = await BuildToolsetWith(BuildProvider(3), Dataset());

        for (var i = 0; i < 3; i++)
        {
            await toolset.ExecuteAsync(
                Call(PowerBIToolset.ListSchemaToolName, "{\"dataset\":\"comercio\"}"), CancellationToken.None);
        }

        // Act
        await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE ROW(1,1)\"}"),
            CancellationToken.None);

        // Assert: as 3 execucoes do orcamento foram usadas, apesar das 3 leituras de schema
        _clientMock.Verify(c => c.ExecuteQueryAsync(
            It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistAndReturnTheDiagnosticWithoutTruncation()
    {
        // Arrange: mensagem acima do antigo limite de 2.000 caracteres (T007)
        var longMessage = new string('x', 4000);
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(400, "DatasetExecuteQueriesError", longMessage, "corpo integral da resposta"));

        PowerBIQueryLog? saved = null;
        _queryLogRepoMock.Setup(r => r.CreateAsync(It.IsAny<PowerBIQueryLog>()))
            .Callback<PowerBIQueryLog>(l => saved = l)
            .ReturnsAsync((PowerBIQueryLog l) => l);

        var toolset = await BuildToolsetWith(BuildProvider(5), Dataset());

        // Act
        var result = await toolset.ExecuteAsync(
            Call(PowerBIToolset.QueryToolName, "{\"dataset\":\"comercio\",\"dax\":\"EVALUATE '[NaoExiste]'\"}"),
            CancellationToken.None);

        // Assert: vai inteiro ao modelo...
        using var doc = JsonDocument.Parse(result);
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal(longMessage, error.GetProperty("message").GetString());
        Assert.Equal(400, error.GetProperty("statusCode").GetInt32());
        Assert.Equal("DatasetExecuteQueriesError", error.GetProperty("errorCode").GetString());
        Assert.Equal("corpo integral da resposta", error.GetProperty("responseBody").GetString());
        Assert.True(error.GetProperty("queryMayBeCorrected").GetBoolean());
        Assert.Equal(PowerBIToolset.CategoryDaxQuery, error.GetProperty("category").GetString());

        // ...e inteiro no historico, como JSON do diagnostico
        _queryLogRepoMock.Verify(r => r.CreateAsync(It.IsAny<PowerBIQueryLog>()), Times.Once);
        Assert.NotNull(saved);
        Assert.Contains(longMessage, saved!.ErrorMessage);
        Assert.True(saved.ErrorMessage!.Length > 2000);
    }
}
