using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.Infra.AppServices;

public class OpenAIService : IOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _embeddingModel;
    private readonly string _chatModel;

    public OpenAIService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
        _embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _chatModel = configuration["OpenAI:ChatModel"] ?? "gpt-4o";
        _client = new OpenAIClient(apiKey);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embeddingClient = _client.GetEmbeddingClient(_embeddingModel);
        var result = await embeddingClient.GenerateEmbeddingAsync(text);
        return result.Value.ToFloats().ToArray();
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_chatModel);

        var chatMessages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        foreach (var msg in messages)
        {
            if (msg.Role == "user")
                chatMessages.Add(new UserChatMessage(msg.Content));
            else if (msg.Role == "assistant")
                chatMessages.Add(new AssistantChatMessage(msg.Content));
        }

        var streamingResult = chatClient.CompleteChatStreamingAsync(chatMessages, cancellationToken: cancellationToken);

        await foreach (var update in streamingResult.WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return part.Text;
                }
            }
        }
    }
}
