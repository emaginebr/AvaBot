namespace AvaBot.Infra.Interfaces.AppServices;

public interface IOpenAIService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<string> ChatCompletionAsync(string model, string systemPrompt, List<ChatCompletionMessage> messages, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatCompletionAsync(string model, string systemPrompt, List<ChatCompletionMessage> messages, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatCompletionWithToolsAsync(string model, string systemPrompt, List<ChatCompletionMessage> messages, IReadOnlyList<ChatToolDefinition> tools, Func<ChatToolCall, CancellationToken, Task<string>> toolExecutor, int maxToolCalls, CancellationToken cancellationToken = default);
    Task<string> ChatCompletionWithToolsAsync(string model, string systemPrompt, List<ChatCompletionMessage> messages, IReadOnlyList<ChatToolDefinition> tools, Func<ChatToolCall, CancellationToken, Task<string>> toolExecutor, int maxToolCalls, CancellationToken cancellationToken = default);
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
