using System.Text.Json;

namespace AvaBot.Calibration;

public class ConversationInput
{
    public string Name { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Arquivo de conversas: { "conversations": [ { "name": "...", "messages": ["...", "..."] } ] }.
/// Cada mensagem e enviada com o historico das anteriores da mesma conversa.
/// </summary>
public static class ConversationFile
{
    public static List<ConversationInput> Parse(string json)
    {
        FileModel? parsed;

        try
        {
            parsed = JsonSerializer.Deserialize<FileModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Arquivo de conversas não é um JSON válido: {ex.Message}");
        }

        if (parsed?.Conversations == null || parsed.Conversations.Count == 0)
            throw new InvalidOperationException("O arquivo de conversas precisa de ao menos uma conversa em \"conversations\".");

        var conversations = new List<ConversationInput>();

        for (var i = 0; i < parsed.Conversations.Count; i++)
        {
            var conversation = parsed.Conversations[i];
            var messages = conversation.Messages?
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.Trim())
                .ToList() ?? new List<string>();

            if (messages.Count == 0)
                throw new InvalidOperationException($"A conversa {i + 1} não tem mensagens.");

            conversations.Add(new ConversationInput
            {
                Name = string.IsNullOrWhiteSpace(conversation.Name) ? $"conversa-{i + 1}" : conversation.Name.Trim(),
                Messages = messages
            });
        }

        return conversations;
    }

    public static List<ConversationInput> FromQuestion(string question) => new()
    {
        new ConversationInput { Name = "conversa-1", Messages = new List<string> { question.Trim() } }
    };

    private sealed class FileModel
    {
        public List<FileConversation>? Conversations { get; set; }
    }

    private sealed class FileConversation
    {
        public string? Name { get; set; }
        public List<string>? Messages { get; set; }
    }
}
