using System.Text.Json;
using AvaBot.Domain.Models;

namespace AvaBot.Application.Services;

/// <summary>
/// Remetente de uma mensagem do webhook do WPP Connect (feature 018, research R2).
/// Em conversa privada o JID vem em "from"; em grupo, "from" e o grupo e o participante vem em "author".
/// </summary>
public class WhatsappSender
{
    public const int MaxProfileNameLength = 120;

    public string WhatsappId { get; set; } = string.Empty;
    /// <summary>Somente digitos, com codigo do pais. Nulo quando o JID e um LID ainda nao resolvido.</summary>
    public string? Phone { get; set; }
    public string? ProfileName { get; set; }
    public bool IsGroup { get; set; }

    public bool IsLid => WhatsappId.EndsWith("@lid", StringComparison.OrdinalIgnoreCase);

    public static WhatsappSender FromPayload(JsonElement payload)
    {
        var isGroup = payload.TryGetProperty("isGroupMsg", out var isGroupProp)
            && isGroupProp.ValueKind == JsonValueKind.True;

        var jid = isGroup ? GetString(payload, "author") : GetString(payload, "from");
        jid ??= string.Empty;

        string? profileName = null;
        if (payload.TryGetProperty("sender", out var sender) && sender.ValueKind == JsonValueKind.Object)
            profileName = FirstNotEmpty(GetString(sender, "pushname"), GetString(payload, "notifyName"), GetString(sender, "formattedName"));
        else
            profileName = FirstNotEmpty(GetString(payload, "notifyName"));

        if (profileName != null && profileName.Length > MaxProfileNameLength)
            profileName = profileName[..MaxProfileNameLength];

        return new WhatsappSender
        {
            WhatsappId = jid,
            Phone = WhatsappPhone.FromJid(jid),
            ProfileName = profileName,
            IsGroup = isGroup
        };
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static string? FirstNotEmpty(params string?[] values) =>
        values.Select(v => v?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));
}
