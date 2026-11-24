namespace AvaBot.Domain.Models;

/// <summary>
/// Normalizacao de telefones para o controle de acesso do WhatsApp (feature 018, research R4).
/// </summary>
public static class WhatsappPhone
{
    public const int MinDigits = 10;
    public const int MaxDigits = 15;
    private const string BrazilCode = "55";

    /// <summary>
    /// So digitos, com codigo do pais. Sem "+" e com 10 ou 11 digitos que nao comecam com 55, assume Brasil.
    /// Devolve null quando o resultado nao tem entre 10 e 15 digitos.
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());

        if (digits.StartsWith("00"))
        {
            digits = digits[2..];
            hasPlus = true;
        }

        if (!hasPlus && (digits.Length == 10 || digits.Length == 11) && !digits.StartsWith(BrazilCode))
            digits = BrazilCode + digits;

        return digits.Length is >= MinDigits and <= MaxDigits ? digits : null;
    }

    /// <summary>
    /// Chave de comparacao: celulares brasileiros (55 + DDD + 9 + 8 digitos) perdem o nono digito,
    /// porque o WhatsApp as vezes identifica o remetente sem ele.
    /// </summary>
    public static string? MatchKey(string? normalized)
    {
        if (string.IsNullOrEmpty(normalized))
            return null;

        if (normalized.Length == 13 && normalized.StartsWith(BrazilCode) && normalized[4] == '9')
            return normalized.Remove(4, 1);

        return normalized;
    }

    /// <summary>Digitos de um JID "...@c.us"; null para "@lid", "@g.us" ou vazio.</summary>
    public static string? FromJid(string? jid)
    {
        if (string.IsNullOrWhiteSpace(jid))
            return null;

        const string suffix = "@c.us";
        if (!jid.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;

        var digits = new string(jid[..^suffix.Length].Where(char.IsAsciiDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }
}
