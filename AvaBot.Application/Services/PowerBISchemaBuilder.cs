using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AvaBot.Application.Services;

public class PowerBISchemaBuilder
{
    private const string TablesDax = "EVALUATE INFO.VIEW.TABLES()";
    private const string ColumnsDax = "EVALUATE INFO.VIEW.COLUMNS()";
    private const string MeasuresDax = "EVALUATE INFO.VIEW.MEASURES()";
    private const string RelationshipsDax = "EVALUATE INFO.VIEW.RELATIONSHIPS()";
    private const string ColumnStatisticsDax = "EVALUATE COLUMNSTATISTICS()";

    // Colunas de texto com ate esta cardinalidade ganham a lista de valores no schema.
    public const int MaxSampleCardinality = 30;
    private const int MaxSampledColumns = 60;

    private readonly IPowerBIClient _powerBIClient;
    private readonly ILogger<PowerBISchemaBuilder> _logger;

    public PowerBISchemaBuilder(IPowerBIClient powerBIClient, ILogger<PowerBISchemaBuilder>? logger = null)
    {
        _powerBIClient = powerBIClient;
        _logger = logger ?? NullLogger<PowerBISchemaBuilder>.Instance;
    }

    public async Task<(PowerBISchema Schema, PowerBISchemaStatus Status)> BuildAsync(
        PowerBICredentials credentials,
        string workspaceId,
        string datasetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tables = await _powerBIClient.ExecuteQueryAsync(credentials, workspaceId, datasetId, TablesDax, cancellationToken);
            var columns = await _powerBIClient.ExecuteQueryAsync(credentials, workspaceId, datasetId, ColumnsDax, cancellationToken);
            var measures = await _powerBIClient.ExecuteQueryAsync(credentials, workspaceId, datasetId, MeasuresDax, cancellationToken);

            var schema = BuildFromInfoViews(tables, columns, measures);

            await AddRelationshipsAsync(schema, credentials, workspaceId, datasetId, cancellationToken);
            await AddSampleValuesAsync(schema, credentials, workspaceId, datasetId, cancellationToken);

            return (schema, PowerBISchemaStatus.Generated);
        }
        catch (PowerBIApiException ex) when (IsInfoViewFailure(ex))
        {
            // Fallback (research R5): COLUMNSTATISTICS exige apenas leitura e nao devolve medidas nem tipos.
            var statistics = await _powerBIClient.ExecuteQueryAsync(
                credentials, workspaceId, datasetId, ColumnStatisticsDax, cancellationToken);

            return (BuildFromColumnStatistics(statistics), PowerBISchemaStatus.Partial);
        }
    }

    private static PowerBISchema BuildFromInfoViews(
        PowerBIQueryResult tables,
        PowerBIQueryResult columns,
        PowerBIQueryResult measures)
    {
        var schema = new PowerBISchema();
        var byName = new Dictionary<string, PowerBISchemaTable>(StringComparer.OrdinalIgnoreCase);

        var tableNameIdx = FindColumn(tables, "Name");
        var tableDescriptionIdx = FindColumn(tables, "Description");
        var tableHiddenIdx = FindColumn(tables, "IsHidden");

        for (var row = 0; row < tables.Rows.Count; row++)
        {
            if (ReadBool(tables, row, tableHiddenIdx)) continue;

            var name = ReadString(tables, row, tableNameIdx);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var table = new PowerBISchemaTable
            {
                Name = name,
                Description = ReadString(tables, row, tableDescriptionIdx)
            };

            schema.Tables.Add(table);
            byName[name] = table;
        }

        var columnTableIdx = FindColumn(columns, "Table");
        var columnNameIdx = FindColumn(columns, "Name");
        var columnTypeIdx = FindColumn(columns, "DataType");
        var columnDescriptionIdx = FindColumn(columns, "Description");
        var columnHiddenIdx = FindColumn(columns, "IsHidden");

        for (var row = 0; row < columns.Rows.Count; row++)
        {
            if (ReadBool(columns, row, columnHiddenIdx)) continue;

            var columnName = ReadString(columns, row, columnNameIdx);
            if (string.IsNullOrWhiteSpace(columnName)) continue;

            // Coluna de tabela oculta ou inexistente nao entra no schema (FR-013).
            if (!byName.TryGetValue(ReadString(columns, row, columnTableIdx) ?? string.Empty, out var table))
                continue;

            table.Columns.Add(new PowerBISchemaColumn
            {
                Name = columnName,
                DataType = ReadString(columns, row, columnTypeIdx),
                Description = ReadString(columns, row, columnDescriptionIdx)
            });
        }

        var measureTableIdx = FindColumn(measures, "Table");
        var measureNameIdx = FindColumn(measures, "Name");
        var measureDescriptionIdx = FindColumn(measures, "Description");
        var measureExpressionIdx = FindColumn(measures, "Expression");
        var measureHiddenIdx = FindColumn(measures, "IsHidden");

        for (var row = 0; row < measures.Rows.Count; row++)
        {
            if (ReadBool(measures, row, measureHiddenIdx)) continue;

            var measureName = ReadString(measures, row, measureNameIdx);
            if (string.IsNullOrWhiteSpace(measureName)) continue;

            if (!byName.TryGetValue(ReadString(measures, row, measureTableIdx) ?? string.Empty, out var table))
                continue;

            table.Measures.Add(new PowerBISchemaMeasure
            {
                Name = measureName,
                Description = ReadString(measures, row, measureDescriptionIdx),
                Expression = ReadString(measures, row, measureExpressionIdx)
            });
        }

        return schema;
    }

    // Enriquecimento opcional: sem relacionamentos o schema basico continua valido.
    private async Task AddRelationshipsAsync(
        PowerBISchema schema, PowerBICredentials credentials, string workspaceId, string datasetId,
        CancellationToken cancellationToken)
    {
        PowerBIQueryResult result;

        try
        {
            result = await _powerBIClient.ExecuteQueryAsync(credentials, workspaceId, datasetId, RelationshipsDax, cancellationToken);
        }
        catch (PowerBIApiException ex)
        {
            _logger.LogWarning("Relacionamentos do dataset {DatasetId} indisponiveis: {Error}", datasetId, ex.Message);
            return;
        }

        var fromTableIdx = FindColumn(result, "FromTable");
        var fromColumnIdx = FindColumn(result, "FromColumn");
        var toTableIdx = FindColumn(result, "ToTable");
        var toColumnIdx = FindColumn(result, "ToColumn");
        var activeIdx = FindColumn(result, "IsActive");

        var visible = new HashSet<string>(schema.Tables.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);

        for (var row = 0; row < result.Rows.Count; row++)
        {
            var fromTable = ReadString(result, row, fromTableIdx);
            var fromColumn = ReadString(result, row, fromColumnIdx);
            var toTable = ReadString(result, row, toTableIdx);
            var toColumn = ReadString(result, row, toColumnIdx);

            if (string.IsNullOrWhiteSpace(fromTable) || string.IsNullOrWhiteSpace(fromColumn)
                || string.IsNullOrWhiteSpace(toTable) || string.IsNullOrWhiteSpace(toColumn))
                continue;

            // Relacionamento com tabela oculta nao serve ao modelo, que nao pode referencia-la.
            if (!visible.Contains(fromTable) || !visible.Contains(toTable))
                continue;

            schema.Relationships.Add(new PowerBISchemaRelationship
            {
                FromTable = fromTable,
                FromColumn = fromColumn,
                ToTable = toTable,
                ToColumn = toColumn,
                IsActive = activeIdx < 0 || ReadBool(result, row, activeIdx)
            });
        }
    }

    // Enriquecimento opcional: valores distintos das colunas de texto de baixa cardinalidade,
    // lidos numa unica consulta (UNION) para nao multiplicar chamadas ao Power BI.
    private async Task AddSampleValuesAsync(
        PowerBISchema schema, PowerBICredentials credentials, string workspaceId, string datasetId,
        CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _powerBIClient.ExecuteQueryAsync(
                credentials, workspaceId, datasetId, ColumnStatisticsDax, cancellationToken);

            var candidates = SelectSampleCandidates(schema, statistics);
            if (candidates.Count == 0)
                return;

            var values = await _powerBIClient.ExecuteQueryAsync(
                credentials, workspaceId, datasetId, BuildSampleValuesDax(candidates), cancellationToken);

            var keyIdx = FindColumn(values, "c");
            var valueIdx = FindColumn(values, "v");
            var byKey = candidates.ToDictionary(c => ColumnKey(c.Table, c.Column.Name), c => c.Column);

            for (var row = 0; row < values.Rows.Count; row++)
            {
                var value = ReadString(values, row, valueIdx);
                if (string.IsNullOrWhiteSpace(value)
                    || !byKey.TryGetValue(ReadString(values, row, keyIdx) ?? string.Empty, out var column))
                    continue;

                column.SampleValues ??= new List<string>();
                column.SampleValues.Add(value);
            }

            foreach (var column in byKey.Values.Where(c => c.SampleValues != null))
                column.SampleValues!.Sort(StringComparer.CurrentCulture);
        }
        catch (PowerBIApiException ex)
        {
            _logger.LogWarning("Valores de exemplo do dataset {DatasetId} indisponiveis: {Error}", datasetId, ex.Message);
        }
    }

    private static List<(string Table, PowerBISchemaColumn Column)> SelectSampleCandidates(
        PowerBISchema schema, PowerBIQueryResult statistics)
    {
        var tableIdx = FindColumn(statistics, "Table Name");
        var columnIdx = FindColumn(statistics, "Column Name");
        var cardinalityIdx = FindColumn(statistics, "Cardinality");

        var cardinality = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        for (var row = 0; row < statistics.Rows.Count; row++)
        {
            var table = ReadString(statistics, row, tableIdx);
            var column = ReadString(statistics, row, columnIdx);

            if (table == null || column == null
                || !long.TryParse(ReadString(statistics, row, cardinalityIdx), out var count))
                continue;

            cardinality[ColumnKey(table, column)] = count;
        }

        return schema.Tables
            .SelectMany(t => t.Columns.Select(c => (Table: t.Name, Column: c)))
            .Where(x => string.Equals(x.Column.DataType, "Text", StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Column.DataType, "String", StringComparison.OrdinalIgnoreCase))
            .Where(x => cardinality.TryGetValue(ColumnKey(x.Table, x.Column.Name), out var count)
                && count is > 1 and <= MaxSampleCardinality)
            .Take(MaxSampledColumns)
            .ToList();
    }

    private static string BuildSampleValuesDax(List<(string Table, PowerBISchemaColumn Column)> candidates)
    {
        var parts = candidates.Select(c =>
        {
            var reference = $"'{c.Table.Replace("'", "''")}'[{c.Column.Name.Replace("]", "]]")}]";
            var key = ColumnKey(c.Table, c.Column.Name).Replace("\"", "\"\"");
            return $"SELECTCOLUMNS(VALUES({reference}), \"c\", \"{key}\", \"v\", {reference} & \"\")";
        }).ToList();

        return parts.Count == 1
            ? $"EVALUATE {parts[0]}"
            : $"EVALUATE UNION({string.Join(", ", parts)})";
    }

    private static string ColumnKey(string table, string column) => $"{table}|{column}";

    private static PowerBISchema BuildFromColumnStatistics(PowerBIQueryResult statistics)
    {
        var schema = new PowerBISchema();
        var byName = new Dictionary<string, PowerBISchemaTable>(StringComparer.OrdinalIgnoreCase);

        var tableNameIdx = FindColumn(statistics, "Table Name");
        var columnNameIdx = FindColumn(statistics, "Column Name");

        for (var row = 0; row < statistics.Rows.Count; row++)
        {
            var tableName = ReadString(statistics, row, tableNameIdx);
            var columnName = ReadString(statistics, row, columnNameIdx);

            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
                continue;

            if (!byName.TryGetValue(tableName, out var table))
            {
                table = new PowerBISchemaTable { Name = tableName };
                schema.Tables.Add(table);
                byName[tableName] = table;
            }

            table.Columns.Add(new PowerBISchemaColumn { Name = columnName });
        }

        return schema;
    }

    private static bool IsInfoViewFailure(PowerBIApiException ex) =>
        ex.StatusCode is 400 or 403 or 404
        || ex.Message.Contains("INFO.VIEW", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("permissão", StringComparison.OrdinalIgnoreCase);

    private static int FindColumn(PowerBIQueryResult result, string suffix)
    {
        for (var i = 0; i < result.Columns.Count; i++)
        {
            var column = result.Columns[i];

            if (string.Equals(column, suffix, StringComparison.OrdinalIgnoreCase))
                return i;

            if (column.EndsWith($"[{suffix}]", StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static object? Cell(PowerBIQueryResult result, int row, int index)
    {
        if (index < 0 || index >= result.Rows[row].Count)
            return null;

        return result.Rows[row][index];
    }

    private static string? ReadString(PowerBIQueryResult result, int row, int index) =>
        Cell(result, row, index)?.ToString();

    private static bool ReadBool(PowerBIQueryResult result, int row, int index) =>
        Cell(result, row, index) switch
        {
            bool value => value,
            string text when bool.TryParse(text, out var parsed) => parsed,
            _ => false
        };
}
