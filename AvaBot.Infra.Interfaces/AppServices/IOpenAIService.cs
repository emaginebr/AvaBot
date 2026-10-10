namespace AvaBot.Infra.Interfaces.AppServices;

public interface IOpenAIService
{
    Task<float[]> GenerateEmbeddingAsync(long agentId, string text);
    Task<string> ChatCompletionAsync(long agentId, string model, string systemPrompt, List<ChatCompletionMessage> messages, CancellationToken cancellationToken = default, ChatCompletionTrace? trace = null);
    IAsyncEnumerable<string> StreamChatCompletionAsync(long agentId, string model, string systemPrompt, List<ChatCompletionMessage> messages, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatCompletionWithToolsAsync(long agentId, string model, string systemPrompt, List<ChatCompletionMessage> messages, IReadOnlyList<ChatToolDefinition> tools, Func<ChatToolCall, CancellationToken, Task<string>> toolExecutor, int maxToolCalls, CancellationToken cancellationToken = default);
    Task<string> ChatCompletionWithToolsAsync(long agentId, string model, string systemPrompt, List<ChatCompletionMessage> messages, IReadOnlyList<ChatToolDefinition> tools, Func<ChatToolCall, CancellationToken, Task<string>> toolExecutor, int maxToolCalls, CancellationToken cancellationToken = default, ChatCompletionTrace? trace = null);
    Task<OpenAIAuthCheckResult> TestApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}

public class OpenAIAuthCheckResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatCompletionMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParametersJsonSchema { get; set; } = string.Empty;
}

public class ChatToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = string.Empty;
}
