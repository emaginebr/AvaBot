using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.Application.Services;

public class PowerBISchemaBuilder
{
    private const string TablesDax = "EVALUATE INFO.VIEW.TABLES()";
    private const string ColumnsDax = "EVALUATE INFO.VIEW.COLUMNS()";
    private const string MeasuresDax = "EVALUATE INFO.VIEW.MEASURES()";
    private const string ColumnStatisticsDax = "EVALUATE COLUMNSTATISTICS()";

    private readonly IPowerBIClient _powerBIClient;

    public PowerBISchemaBuilder(IPowerBIClient powerBIClient)
    {
        _powerBIClient = powerBIClient;
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

            return (BuildFromInfoViews(tables, columns, measures), PowerBISchemaStatus.Generated);
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
                Description = ReadString(measures, row, measureDescriptionIdx)
            });
        }

        return schema;
    }

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
