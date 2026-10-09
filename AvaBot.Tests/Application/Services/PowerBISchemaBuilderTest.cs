using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using Moq;
using Xunit;

namespace AvaBot.Tests.Application.Services;

public class PowerBISchemaBuilderTest
{
    private readonly Mock<IPowerBIClient> _clientMock = new();
    private readonly PowerBISchemaBuilder _sut;

    public PowerBISchemaBuilderTest()
    {
        _sut = new PowerBISchemaBuilder(_clientMock.Object);
    }

    private static PowerBICredentials Credentials() => new()
    {
        TenantId = "tenant",
        ClientId = "client",
        ClientSecret = "secret"
    };

    private static PowerBIQueryResult FromRows(IReadOnlyList<string> columns, params object?[][] rows) => new()
    {
        Columns = columns.ToList(),
        Rows = rows.Select(r => r.ToList()).ToList()
    };

    private void SetupInfoViews(PowerBIQueryResult tables, PowerBIQueryResult columns, PowerBIQueryResult measures)
    {
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.TABLES()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tables);
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.COLUMNS()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(columns);
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.MEASURES()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(measures);
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.RELATIONSHIPS()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FromRows(new[] { "[FromTable]", "[FromColumn]", "[ToTable]", "[ToColumn]", "[IsActive]" }));
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE COLUMNSTATISTICS()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FromRows(new[] { "[Table Name]", "[Column Name]", "[Cardinality]" }));
    }

    [Fact]
    public async Task BuildAsync_ShouldAddExpressionsRelationshipsAndSampleValues()
    {
        // Arrange: fatos + dimensao SH6; so a coluna de texto de baixa cardinalidade ganha valores
        SetupInfoViews(
            FromRows(
                new[] { "[Name]", "[IsHidden]" },
                new object?[] { "public COMTRADE", false },
                new object?[] { "SH6", false },
                new object?[] { "Oculta", true }),
            FromRows(
                new[] { "[Table]", "[Name]", "[DataType]", "[IsHidden]" },
                new object?[] { "public COMTRADE", "cmdCode", "Text", false },
                new object?[] { "SH6", "id", "Text", false },
                new object?[] { "SH6", "ABIPESCA", "Text", false },
                new object?[] { "SH6", "aggrLevel", "Integer", false }),
            FromRows(
                new[] { "[Table]", "[Name]", "[Expression]", "[IsHidden]" },
                new object?[] { "SH6", "Exportação (Peso Kg)", "CALCULATE(SUM(x), y = \"X\")", false }));

        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.RELATIONSHIPS()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FromRows(
                new[] { "[FromTable]", "[FromColumn]", "[ToTable]", "[ToColumn]", "[IsActive]" },
                new object?[] { "public COMTRADE", "cmdCode", "SH6", "id", true },
                new object?[] { "public COMTRADE", "cmdCode", "Oculta", "id", true }));

        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE COLUMNSTATISTICS()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FromRows(
                new[] { "[Table Name]", "[Column Name]", "[Cardinality]" },
                new object?[] { "public COMTRADE", "cmdCode", 5000 },
                new object?[] { "SH6", "id", 5000 },
                new object?[] { "SH6", "ABIPESCA", 3 },
                new object?[] { "SH6", "aggrLevel", 4 }));

        string? sampleDax = null;
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds",
                It.Is<string>(q => q.Contains("SELECTCOLUMNS")), It.IsAny<CancellationToken>()))
            .Callback<PowerBICredentials, string, string, string, CancellationToken>((_, _, _, q, _) => sampleDax = q)
            .ReturnsAsync(FromRows(
                new[] { "[c]", "[v]" },
                new object?[] { "SH6|ABIPESCA", "Tilápia" },
                new object?[] { "SH6|ABIPESCA", "Camarão" },
                new object?[] { "SH6|ABIPESCA", "" }));

        // Act
        var (schema, status) = await _sut.BuildAsync(Credentials(), "ws", "ds");

        // Assert
        Assert.Equal(PowerBISchemaStatus.Generated, status);
        Assert.Equal("CALCULATE(SUM(x), y = \"X\")", schema.Tables[1].Measures.Single().Expression);

        var relationship = Assert.Single(schema.Relationships);
        Assert.Equal(("public COMTRADE", "cmdCode", "SH6", "id"),
            (relationship.FromTable, relationship.FromColumn, relationship.ToTable, relationship.ToColumn));

        Assert.Equal("EVALUATE SELECTCOLUMNS(VALUES('SH6'[ABIPESCA]), \"c\", \"SH6|ABIPESCA\", \"v\", 'SH6'[ABIPESCA] & \"\")", sampleDax);
        Assert.Equal(new[] { "Camarão", "Tilápia" }, schema.Tables[1].Columns.Single(c => c.Name == "ABIPESCA").SampleValues);
        Assert.Null(schema.Tables[1].Columns.Single(c => c.Name == "id").SampleValues);
    }

    [Fact]
    public async Task BuildAsync_ShouldKeepBasicSchema_WhenEnrichmentQueriesFail()
    {
        // Arrange
        SetupInfoViews(
            FromRows(new[] { "[Name]" }, new object?[] { "T" }),
            FromRows(new[] { "[Table]", "[Name]", "[DataType]" }, new object?[] { "T", "C", "Text" }),
            FromRows(new[] { "[Table]", "[Name]" }));

        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.RELATIONSHIPS()", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(400, "DatasetExecuteQueriesError", "not supported"));
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE COLUMNSTATISTICS()", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(400, "DatasetExecuteQueriesError", "not supported"));

        // Act
        var (schema, status) = await _sut.BuildAsync(Credentials(), "ws", "ds");

        // Assert
        Assert.Equal(PowerBISchemaStatus.Generated, status);
        Assert.Equal("C", schema.Tables.Single().Columns.Single().Name);
        Assert.Empty(schema.Relationships);
    }

    [Fact]
    public async Task BuildAsync_ShouldReadColumnsBySuffixAndSkipHidden_WhenInfoViewsAreAvailable()
    {
        // Arrange: nomes qualificados (Tabela[Coluna]) e itens ocultos que devem ser ignorados (FR-013)
        SetupInfoViews(
            FromRows(
                new[] { "Table[Name]", "Table[Description]", "Table[IsHidden]" },
                new object?[] { "Exportacoes", "Exportações por país", false },
                new object?[] { "Parametros", null, true }),
            FromRows(
                new[] { "Column[Table]", "Column[Name]", "Column[DataType]", "Column[Description]", "Column[IsHidden]" },
                new object?[] { "Exportacoes", "Data", "DateTime", "use para periodo", false },
                new object?[] { "Exportacoes", "Temporaria", "Int64", null, true },
                new object?[] { "Parametros", "Oculta", "String", null, false }),
            FromRows(
                new[] { "Measure[Table]", "Measure[Name]", "Measure[Description]", "Measure[IsHidden]" },
                new object?[] { "Exportacoes", "Valor FOB (US$)", null, false },
                new object?[] { "Exportacoes", "Interna", null, true }));

        // Act
        var (schema, status) = await _sut.BuildAsync(Credentials(), "ws", "ds");

        // Assert
        Assert.Equal(PowerBISchemaStatus.Generated, status);
        var table = Assert.Single(schema.Tables);
        Assert.Equal("Exportacoes", table.Name);
        Assert.Equal("Exportações por país", table.Description);
        Assert.Equal(new[] { "Data" }, table.Columns.Select(c => c.Name));
        Assert.Equal("DateTime", table.Columns.Single().DataType);
        Assert.Equal("use para periodo", table.Columns.Single().Description);
        Assert.Equal(new[] { "Valor FOB (US$)" }, table.Measures.Select(m => m.Name));
    }

    [Fact]
    public async Task BuildAsync_ShouldReturnPartialAndNoMeasures_WhenItFallsBackToColumnStatistics()
    {
        // Arrange (research R5): INFO.VIEW indisponivel -> COLUMNSTATISTICS so traz tabela e coluna
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.TABLES()", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(400, "AXExternalError", "The function 'INFO.VIEW.TABLES' is not recognized."));

        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE COLUMNSTATISTICS()", It.IsAny<CancellationToken>()))
            .ReturnsAsync(FromRows(
                new[] { "Table Name", "Column Name", "Cardinality" },
                new object?[] { "Exportacoes", "Data", 100 },
                new object?[] { "Exportacoes", "Pais", 30 },
                new object?[] { "Importacoes", "Data", 12 }));

        // Act
        var (schema, status) = await _sut.BuildAsync(Credentials(), "ws", "ds");

        // Assert
        Assert.Equal(PowerBISchemaStatus.Partial, status);
        Assert.Equal(2, schema.Tables.Count);
        Assert.Equal("Data", schema.Tables[0].Columns[0].Name);
        Assert.Null(schema.Tables[0].Columns[0].DataType);
        Assert.Empty(schema.Tables[0].Measures);
    }

    [Fact]
    public async Task BuildAsync_ShouldPropagatePermissionError_WhenFallbackAlsoFails()
    {
        // Arrange
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE INFO.VIEW.TABLES()", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(404, "PowerBIEntityNotFound", "no query permission"));
        _clientMock.Setup(c => c.ExecuteQueryAsync(It.IsAny<PowerBICredentials>(), "ws", "ds", "EVALUATE COLUMNSTATISTICS()", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PowerBIApiException(404, "PowerBIEntityNotFound", "no query permission"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<PowerBIApiException>(() => _sut.BuildAsync(Credentials(), "ws", "ds"));
        Assert.Equal("PowerBIEntityNotFound", exception.ErrorCode);
    }

    [Fact]
    public void MergeUserDescriptions_ShouldKeepAdminDescriptionsAndDropRemovedItems()
    {
        // Arrange
        var previous = new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new()
                {
                    Name = "exportacoes",
                    UserDescription = "descrição do administrador",
                    Columns = new List<PowerBISchemaColumn>
                    {
                        new() { Name = "Data", UserDescription = "use por periodo" },
                        new() { Name = "Removida", UserDescription = "nao existe mais" }
                    },
                    Measures = new List<PowerBISchemaMeasure>
                    {
                        new() { Name = "Valor FOB (US$)", UserDescription = "em dolares" }
                    }
                }
            }
        };

        var fresh = new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new()
                {
                    Name = "Exportacoes",
                    Description = "descricao do modelo",
                    Columns = new List<PowerBISchemaColumn>
                    {
                        new() { Name = "DATA" },
                        new() { Name = "Pais" }
                    },
                    Measures = new List<PowerBISchemaMeasure>
                    {
                        new() { Name = "valor fob (us$)" }
                    }
                },
                new() { Name = "NovaTabela" }
            }
        };

        // Act
        var merged = PowerBISchema.MergeUserDescriptions(previous, fresh);

        // Assert: match case-insensitive por tabela, coluna e medida (FR-012)
        var table = merged.Tables.First(t => t.Name == "Exportacoes");
        Assert.Equal("descrição do administrador", table.UserDescription);
        Assert.Equal("descricao do modelo", table.Description);
        Assert.Equal("use por periodo", table.Columns.First(c => c.Name == "DATA").UserDescription);
        Assert.Null(table.Columns.First(c => c.Name == "Pais").UserDescription);
        Assert.Equal("em dolares", table.Measures.Single().UserDescription);
        Assert.Null(merged.Tables.Single(t => t.Name == "NovaTabela").UserDescription);
    }

    [Fact]
    public void MergeUserDescriptions_ShouldNotOverwriteDescriptionAlreadyInFreshSchema()
    {
        // Arrange
        var previous = new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new() { Name = "T", UserDescription = "antiga" }
            }
        };

        var fresh = new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new() { Name = "T", UserDescription = "nova" }
            }
        };

        // Act
        var merged = PowerBISchema.MergeUserDescriptions(previous, fresh);

        // Assert
        Assert.Equal("nova", merged.Tables.Single().UserDescription);
    }

    [Fact]
    public void Serialize_Deserialize_ShouldRoundTripTheSchemaJson()
    {
        // Arrange
        var schema = new PowerBISchema
        {
            Tables = new List<PowerBISchemaTable>
            {
                new()
                {
                    Name = "Exportacoes",
                    UserDescription = "descricao do admin",
                    Columns = new List<PowerBISchemaColumn> { new() { Name = "Data", DataType = "DateTime" } }
                }
            }
        };

        // Act
        var json = PowerBISchema.Serialize(schema);
        var restored = PowerBISchema.Deserialize(json);

        // Assert
        Assert.Contains("\"userDescription\"", json);
        Assert.Equal("descricao do admin", restored!.Tables.Single().UserDescription);
        Assert.Equal("DateTime", restored.Tables.Single().Columns.Single().DataType);
        Assert.Null(PowerBISchema.Deserialize(null));
        Assert.Null(PowerBISchema.Deserialize("não é json"));
    }
}
