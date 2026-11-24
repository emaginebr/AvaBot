using System.Globalization;
using System.Text;

namespace AvaBot.Domain.Models;

public enum WhatsappReply
{
    None,
    Yes,
    No
}

/// <summary>Reconhece "sim"/"nao" sem IA (feature 018, research R5).</summary>
public static class WhatsappReplyParser
{
    public static WhatsappReply Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return WhatsappReply.None;

        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetter(c))
                builder.Append(c);
        }

        return builder.ToString() switch
        {
            "sim" or "s" => WhatsappReply.Yes,
            "nao" or "n" => WhatsappReply.No,
            _ => WhatsappReply.None
        };
    }
}
