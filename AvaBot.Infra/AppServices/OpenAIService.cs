using System.ClientModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using AvaChatToolCall = AvaBot.Infra.Interfaces.AppServices.ChatToolCall;

namespace AvaBot.Infra.AppServices;

public class OpenAIService : IOpenAIService
{
    private readonly IAgentRepository<Agent> _agentRepository;
    private readonly ISecretProtector _secretProtector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _embeddingModel;
    private readonly string _openAIBaseUrl;

    public OpenAIService(
        IAgentRepository<Agent> agentRepository,
        ISecretProtector secretProtector,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _agentRepository = agentRepository;
        _secretProtector = secretProtector;
        _httpClientFactory = httpClientFactory;
        _embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _openAIBaseUrl = (configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
    }

    // A credencial e resolvida por agente a cada operacao: nenhum cliente e
    // guardado em campo, entao uma chave nunca fica viva em memoria alem da chamada (D3).
    private async Task<OpenAIClient> ResolveClientAsync(long agentId)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId)
            ?? throw new InvalidOperationException("Agente nao encontrado para usar os recursos de IA.");

        if (string.IsNullOrEmpty(agent.OpenAIApiKeyEncrypted))
            throw new InvalidOperationException("Este agente ainda nao possui uma chave OpenAI configurada.");

        var apiKey = _secretProtector.Unprotect(agent.OpenAIApiKeyEncrypted);
        return new OpenAIClient(apiKey);
    }

    private async Task<ChatClient> ResolveChatClientAsync(long agentId, string model)
    {
        var client = await ResolveClientAsync(agentId);
        return client.GetChatClient(model);
    }

    public async Task<float[]> GenerateEmbeddingAsync(long agentId, string text)
    {
        var client = await ResolveClientAsync(agentId);
        var embeddingClient = client.GetEmbeddingClient(_embeddingModel);
        var result = await embeddingClient.GenerateEmbeddingAsync(text);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<string> ChatCompletionAsync(
        long agentId,
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        CancellationToken cancellationToken = default,
        ChatCompletionTrace? trace = null)
    {
        var chatClient = await ResolveChatClientAsync(agentId, model);
        var chatMessages = BuildChatMessages(systemPrompt, messages);

        var round = StartRound(trace, chatMessages, toolsOffered: false, toolChoiceNone: false);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken);
            stopwatch.Stop();

            CompleteRound(round, result.Value, stopwatch.ElapsedMilliseconds);
            return result.Value.Content[0].Text;
        }
        catch (Exception ex) when (trace != null && !cancellationToken.IsCancellationRequested)
        {
            trace.Error = ex.Message;
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        long agentId,
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = await ResolveChatClientAsync(agentId, model);
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
        long agentId,
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        Func<AvaChatToolCall, CancellationToken, Task<string>> toolExecutor,
        int maxToolCalls,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = await ResolveChatClientAsync(agentId, model);
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
        long agentId,
        string model,
        string systemPrompt,
        List<ChatCompletionMessage> messages,
        IReadOnlyList<ChatToolDefinition> tools,
        Func<AvaChatToolCall, CancellationToken, Task<string>> toolExecutor,
        int maxToolCalls,
        CancellationToken cancellationToken = default,
        ChatCompletionTrace? trace = null)
    {
        var chatClient = await ResolveChatClientAsync(agentId, model);
        var chatMessages = BuildChatMessages(systemPrompt, messages);
        var executedToolCalls = 0;

        try
        {
            while (true)
            {
                var forceFinalAnswer = executedToolCalls >= maxToolCalls;
                var options = BuildToolOptions(tools, forceFinalAnswer);
                var round = StartRound(trace, chatMessages, toolsOffered: tools.Count > 0, toolChoiceNone: forceFinalAnswer);

                var stopwatch = Stopwatch.StartNew();
                var result = await chatClient.CompleteChatAsync(chatMessages, options, cancellationToken);
                stopwatch.Stop();

                var completion = result.Value;
                CompleteRound(round, completion, stopwatch.ElapsedMilliseconds);

                if (completion.FinishReason != ChatFinishReason.ToolCalls || completion.ToolCalls.Count == 0)
                    return completion.Content[0].Text;

                chatMessages.Add(new AssistantChatMessage(completion.ToolCalls));

                foreach (var toolCall in completion.ToolCalls)
                {
                    executedToolCalls++;

                    var toolStopwatch = Stopwatch.StartNew();
                    var toolResult = await ExecuteToolAsync(toolExecutor, toolCall, cancellationToken);
                    toolStopwatch.Stop();

                    var traced = round?.ToolCalls.Find(c => c.Id == toolCall.Id);
                    if (traced != null)
                    {
                        traced.Result = toolResult;
                        traced.DurationMs = toolStopwatch.ElapsedMilliseconds;
                    }

                    chatMessages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                }
            }
        }
        catch (Exception ex) when (trace != null && !cancellationToken.IsCancellationRequested)
        {
            trace.Error = ex.Message;
            throw;
        }
    }

    // ---------- Rastreamento (teste de agente / calibracao) ----------

    // A rodada guarda uma copia das mensagens: a lista do loop continua crescendo depois.
    private static ChatTraceRound? StartRound(
        ChatCompletionTrace? trace, List<OpenAI.Chat.ChatMessage> chatMessages, bool toolsOffered, bool toolChoiceNone)
    {
        if (trace == null)
            return null;

        var round = new ChatTraceRound
        {
            Number = trace.Rounds.Count + 1,
            Messages = chatMessages.Select(ToTraceMessage).ToList(),
            ToolsOffered = toolsOffered,
            ToolChoiceNone = toolChoiceNone
        };

        trace.Rounds.Add(round);
        return round;
    }

    private static void CompleteRound(ChatTraceRound? round, ChatCompletion completion, long durationMs)
    {
        if (round == null)
            return;

        round.DurationMs = durationMs;
        round.FinishReason = FinishReasonText(completion.FinishReason);

        var text = ContentText(completion.Content);
        round.ResponseText = string.IsNullOrEmpty(text) ? null : text;
        round.ToolCalls = completion.ToolCalls.Select(ToTraceToolCall).ToList();
        round.InputTokens = completion.Usage?.InputTokenCount;
        round.OutputTokens = completion.Usage?.OutputTokenCount;
    }

    private static ChatTraceMessage ToTraceMessage(OpenAI.Chat.ChatMessage message) => message switch
    {
        SystemChatMessage system => new ChatTraceMessage { Role = "system", Content = ContentText(system.Content) },
        UserChatMessage user => new ChatTraceMessage { Role = "user", Content = ContentText(user.Content) },
        AssistantChatMessage assistant => new ChatTraceMessage
        {
            Role = "assistant",
            Content = ContentText(assistant.Content),
            ToolCalls = assistant.ToolCalls.Count > 0 ? assistant.ToolCalls.Select(ToTraceToolCall).ToList() : null
        },
        ToolChatMessage tool => new ChatTraceMessage
        {
            Role = "tool",
            Content = ContentText(tool.Content),
            ToolCallId = tool.ToolCallId
        },
        _ => new ChatTraceMessage { Role = "unknown", Content = ContentText(message.Content) }
    };

    private static ChatTraceToolCall ToTraceToolCall(OpenAI.Chat.ChatToolCall toolCall) => new()
    {
        Id = toolCall.Id,
        Name = toolCall.FunctionName,
        ArgumentsJson = toolCall.FunctionArguments?.ToString() ?? "{}"
    };

    private static string ContentText(ChatMessageContent? content) =>
        content == null
            ? string.Empty
            : string.Concat(content.Where(p => p.Kind == ChatMessageContentPartKind.Text).Select(p => p.Text));

    private static string FinishReasonText(ChatFinishReason reason) => reason switch
    {
        ChatFinishReason.Stop => "stop",
        ChatFinishReason.Length => "length",
        ChatFinishReason.ContentFilter => "content_filter",
        ChatFinishReason.ToolCalls => "tool_calls",
        ChatFinishReason.FunctionCall => "function_call",
        _ => reason.ToString().ToLowerInvariant()
    };

    // D4: listar modelos e uma operacao autenticada sem geracao de conteudo,
    // entao o diagnostico confirma a chave sem gastar tokens.
    public async Task<OpenAIAuthCheckResult> TestApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var key = apiKey?.Trim() ?? string.Empty;

        if (key.Length == 0)
        {
            return new OpenAIAuthCheckResult
            {
                Success = false,
                Message = "Nenhuma chave informada. Digite a chave ou salve uma credencial para este agente."
            };
        }

        var client = _httpClientFactory.CreateClient("OpenAI");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_openAIBaseUrl}/models");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return new OpenAIAuthCheckResult { Success = true, Message = "A chave autenticou com sucesso." };

            // O corpo do provedor nao e lido: a mensagem nao pode ecoar nada da resposta externa.
            return new OpenAIAuthCheckResult
            {
                Success = false,
                Message = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "A OpenAI recusou a chave (401). Confirme o valor e se ela pertence a esta conta.",
                    HttpStatusCode.Forbidden => "A chave autenticou, mas nao tem permissao para listar modelos (403).",
                    HttpStatusCode.TooManyRequests => "A OpenAI limitou as requisicoes agora (429). Tente novamente em instantes.",
                    _ => $"A OpenAI respondeu com status {(int)response.StatusCode}. A autenticacao nao foi confirmada."
                }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return new OpenAIAuthCheckResult
            {
                Success = false,
                Message = "Nao foi possivel falar com a OpenAI a partir deste servidor. Verifique a rede e o acesso a api.openai.com."
            };
        }
        catch (TaskCanceledException)
        {
            return new OpenAIAuthCheckResult
            {
                Success = false,
                Message = "A OpenAI nao respondeu dentro do tempo esperado. Tente novamente."
            };
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
