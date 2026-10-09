using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvaBot.Domain.Models;

public class PowerBISchema
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("tables")]
    public List<PowerBISchemaTable> Tables { get; set; } = new List<PowerBISchemaTable>();

    [JsonPropertyName("relationships")]
    public List<PowerBISchemaRelationship> Relationships { get; set; } = new List<PowerBISchemaRelationship>();

    public static string Serialize(PowerBISchema schema)
        => JsonSerializer.Serialize(schema, SerializerOptions);

    public static PowerBISchema? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PowerBISchema>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Mantém as descrições do administrador quando a chave (tabela, tabela+coluna,
    // tabela+medida) já existia no schema anterior. Itens que sumiram são descartados (FR-012).
    public static PowerBISchema MergeUserDescriptions(PowerBISchema previous, PowerBISchema fresh)
    {
        foreach (var table in fresh.Tables)
        {
            var previousTable = previous.Tables
                .FirstOrDefault(t => string.Equals(t.Name, table.Name, StringComparison.OrdinalIgnoreCase));

            if (previousTable == null) continue;

            table.UserDescription ??= previousTable.UserDescription;

            foreach (var column in table.Columns)
            {
                var previousColumn = previousTable.Columns
                    .FirstOrDefault(c => string.Equals(c.Name, column.Name, StringComparison.OrdinalIgnoreCase));

                if (previousColumn != null)
                    column.UserDescription ??= previousColumn.UserDescription;
            }

            foreach (var measure in table.Measures)
            {
                var previousMeasure = previousTable.Measures
                    .FirstOrDefault(m => string.Equals(m.Name, measure.Name, StringComparison.OrdinalIgnoreCase));

                if (previousMeasure != null)
                    measure.UserDescription ??= previousMeasure.UserDescription;
            }
        }

        return fresh;
    }
}

public class PowerBISchemaTable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }

    [JsonPropertyName("columns")]
    public List<PowerBISchemaColumn> Columns { get; set; } = new List<PowerBISchemaColumn>();

    [JsonPropertyName("measures")]
    public List<PowerBISchemaMeasure> Measures { get; set; } = new List<PowerBISchemaMeasure>();
}

public class PowerBISchemaColumn
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }

    // Valores distintos de colunas de texto com poucos valores (ex.: grupo de espécie),
    // para o modelo filtrar pelo valor exato sem ter que listar a tabela.
    [JsonPropertyName("sampleValues")]
    public List<string>? SampleValues { get; set; }
}

public class PowerBISchemaMeasure
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }

    // Formula DAX da medida: mostra ao modelo quais filtros ela ja aplica.
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }
}

public class PowerBISchemaRelationship
{
    [JsonPropertyName("fromTable")]
    public string FromTable { get; set; } = string.Empty;

    [JsonPropertyName("fromColumn")]
    public string FromColumn { get; set; } = string.Empty;

    [JsonPropertyName("toTable")]
    public string ToTable { get; set; } = string.Empty;

    [JsonPropertyName("toColumn")]
    public string ToColumn { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
