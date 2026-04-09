namespace Avachat.Infra.Interfaces.AppServices;

public interface IOpenAIService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<string> ChatCompletionAsync(string systemPrompt, List<ChatCompletionMessage> messages, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatCompletionAsync(string systemPrompt, List<ChatCompletionMessage> messages, CancellationToken cancellationToken = default);
}

public class ChatCompletionMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
