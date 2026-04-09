using System.Text.Json.Serialization;

namespace Avachat.DTO;

public class AgentInfo
{
    [JsonPropertyName("agentId")]
    public long AgentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class AgentInsertInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }
}

public class AgentChatConfigInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("collectName")]
    public bool CollectName { get; set; }

    [JsonPropertyName("collectEmail")]
    public bool CollectEmail { get; set; }

    [JsonPropertyName("collectPhone")]
    public bool CollectPhone { get; set; }
}

public class AgentTestQuestionInfo
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}

public class AgentTestResultInfo
{
    [JsonPropertyName("searchQuery")]
    public string SearchQuery { get; set; } = string.Empty;

    [JsonPropertyName("searchResults")]
    public List<string> SearchResults { get; set; } = new();

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<AgentTestMessageInfo> Messages { get; set; } = new();

    [JsonPropertyName("assistantResponse")]
    public string AssistantResponse { get; set; } = string.Empty;
}

public class AgentTestMessageInfo
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class Result<T>
{
    [JsonPropertyName("sucesso")]
    public bool Sucesso { get; set; }

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = string.Empty;

    [JsonPropertyName("erros")]
    public string[] Erros { get; set; } = Array.Empty<string>();

    [JsonPropertyName("dados")]
    public T? Dados { get; set; }

    public static Result<T> Success(T data, string message = "Operacao realizada com sucesso")
    {
        return new Result<T> { Sucesso = true, Mensagem = message, Dados = data };
    }

    public static Result<T> Failure(string message, string[]? errors = null)
    {
        return new Result<T> { Sucesso = false, Mensagem = message, Erros = errors ?? Array.Empty<string>() };
    }
}
