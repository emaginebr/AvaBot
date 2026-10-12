using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AvaBot.API.Mcp;

/// <summary>Serializacao das ferramentas MCP: camelCase, sem nulos, enums como texto.</summary>
public static class McpJson
{
    // O SDK marca as opcoes como somente leitura ao gerar o schema das ferramentas; sem um
    // TypeInfoResolver explicito isso lanca InvalidOperationException e a API nao sobe.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };
}
