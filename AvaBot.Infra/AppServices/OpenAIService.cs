using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using AvaBot.Infra.Interfaces.AppServices;
using AvaChatToolCall = AvaBot.Infra.Interfaces.AppServices.ChatToolCall;

namespace AvaBot.Infra.AppServices;

public class OpenAIService : IOpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _embeddingModel;

    public OpenAIService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
        _embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _client = new OpenAIClient(apiKey);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var embeddingClient = _client.GetEmbeddingClient(_embeddingModel);
        var result = await embeddingClient.GenerateEmbeddingAsync(text);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<string> ChatCompletionAsync(
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(model);
        var chatMessages = BuildChatMessages(systemPrompt, messages);

        var result = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken);
        return result.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(model);
        var chatMessages = BuildChatMessages(systemPrompt, messages);

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

    public async IAsyncEnumerable<string> StreamChatCompletionWithToolsAsync(
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        Func<AvaChatToolCall, CancellationToken, Task<string>> toolExecutor,
        int maxToolCalls,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(model);
        var chatMessages = BuildChatMessages(systemPrompt, messages);
        var executedToolCalls = 0;

        while (true)
        {
            // Ao atingir o limite, a proxima rodada vai sem tools (ToolChoice = none), forcando a resposta final (R2).
            var options = BuildToolOptions(tools, executedToolCalls >= maxToolCalls);
            var pending = new List<PendingToolCall>();
            ChatFinishReason? finishReason = null;

            var streamingResult = chatClient.CompleteChatStreamingAsync(chatMessages, options, cancellationToken);

            await foreach (var update in streamingResult.WithCancellation(cancellationToken))
            {
                if (update.FinishReason.HasValue)
                    finishReason = update.FinishReason;

                foreach (var part in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                        yield return part.Text;
                }

                foreach (var toolCallUpdate in update.ToolCallUpdates)
                    AccumulateToolCall(pending, toolCallUpdate);
            }

            if (finishReason != ChatFinishReason.ToolCalls || pending.Count == 0)
                break;

            var toolCalls = new List<OpenAI.Chat.ChatToolCall>();

            foreach (var entry in pending)
            {
                if (string.IsNullOrWhiteSpace(entry.Name)) continue;

                toolCalls.Add(OpenAI.Chat.ChatToolCall.CreateFunctionToolCall(
                    entry.Id,
                    entry.Name,
                    BinaryData.FromString(entry.Arguments.Length > 0 ? entry.Arguments.ToString() : "{}")));
            }

            if (toolCalls.Count == 0)
                break;

            chatMessages.Add(new AssistantChatMessage(toolCalls));

            foreach (var toolCall in toolCalls)
            {
                executedToolCalls++;
                var toolResult = await ExecuteToolAsync(toolExecutor, toolCall, cancellationToken);
                chatMessages.Add(new ToolChatMessage(toolCall.Id, toolResult));
            }
        }
    }

    public async Task<string> ChatCompletionWithToolsAsync(
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        Func<AvaChatToolCall, CancellationToken, Task<string>> toolExecutor,
        int maxToolCalls,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(model);
        var chatMessages = BuildChatMessages(systemPrompt, messages);
        var executedToolCalls = 0;

        while (true)
        {
            var options = BuildToolOptions(tools, executedToolCalls >= maxToolCalls);
            var result = await chatClient.CompleteChatAsync(chatMessages, options, cancellationToken);
            var completion = result.Value;

            if (completion.FinishReason != ChatFinishReason.ToolCalls || completion.ToolCalls.Count == 0)
                return completion.Content[0].Text;

            chatMessages.Add(new AssistantChatMessage(completion.ToolCalls));

            foreach (var toolCall in completion.ToolCalls)
            {
                executedToolCalls++;
                var toolResult = await ExecuteToolAsync(toolExecutor, toolCall, cancellationToken);
                chatMessages.Add(new ToolChatMessage(toolCall.Id, toolResult));
            }
        }
    }

    private static List<OpenAI.Chat.ChatMessage> BuildChatMessages(string systemPrompt, List<ChatCompletionMessage> messages)
    {
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
            else if (msg.Role == "system")
                chatMessages.Add(new SystemChatMessage(msg.Content));
        }

        return chatMessages;
    }

    private static ChatCompletionOptions BuildToolOptions(IReadOnlyList<ChatToolDefinition> tools, bool forceFinalAnswer)
    {
        var options = new ChatCompletionOptions();

        foreach (var tool in tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                BinaryData.FromString(tool.ParametersJsonSchema)));
        }

        if (forceFinalAnswer)
            options.ToolChoice = ChatToolChoice.CreateNoneChoice();

        return options;
    }

    private static async Task<string> ExecuteToolAsync(
        Func<AvaChatToolCall, CancellationToken, Task<string>> toolExecutor,
        OpenAI.Chat.ChatToolCall toolCall,
        CancellationToken cancellationToken)
    {
        try
        {
            return await toolExecutor(
                new AvaChatToolCall
                {
                    Id = toolCall.Id,
                    Name = toolCall.FunctionName,
                    ArgumentsJson = toolCall.FunctionArguments?.ToString() ?? "{}"
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // O erro vira payload para o modelo corrigir a consulta sem quebrar a conversa (FR-026).
            return JsonSerializer.Serialize(new Dictionary<string, string> { ["error"] = ex.Message });
        }
    }

    // O StreamingChatToolCallUpdate do SDK 2.10 nao expoe Index, entao a acumulacao usa o ToolCallId:
    // um id novo abre uma chamada e os updates seguintes completam a chamada em aberto.
    private static void AccumulateToolCall(List<PendingToolCall> pending, StreamingChatToolCallUpdate update)
    {
        PendingToolCall? target = null;

        if (!string.IsNullOrEmpty(update.ToolCallId))
            target = pending.Find(p => p.Id == update.ToolCallId);

        target ??= pending.Count > 0 && string.IsNullOrEmpty(update.ToolCallId)
            ? pending[^1]
            : null;

        if (target == null)
        {
            target = new PendingToolCall { Id = update.ToolCallId ?? string.Empty };
            pending.Add(target);
        }

        if (!string.IsNullOrEmpty(update.FunctionName))
            target.Name = update.FunctionName;

        var arguments = update.FunctionArgumentsUpdate?.ToString();
        if (!string.IsNullOrEmpty(arguments))
            target.Arguments.Append(arguments);
    }

    private sealed class PendingToolCall
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public StringBuilder Arguments { get; } = new();
    }
}
