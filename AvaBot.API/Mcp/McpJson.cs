using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvaBot.API.Mcp;

/// <summary>Serializacao das ferramentas MCP: camelCase, sem nulos, enums como texto.</summary>
public static class McpJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
