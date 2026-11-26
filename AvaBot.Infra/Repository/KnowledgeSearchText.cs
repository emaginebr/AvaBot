using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AvaBot.Infra.Repository;

/// <summary>
/// Normalizacao do texto e montagem da consulta da busca textual (feature 021). O mesmo
/// <see cref="Normalize"/> vale para o trecho gravado e para a pergunta, entao acento e caixa nao
/// mudam o resultado.
/// </summary>
public static class KnowledgeSearchText
{
    public const int MaxTerms = 64;

    private static readonly Regex TermPattern = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    /// <summary>Minusculas e sem acentos ("Ação" vira "acao").</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Termos da pergunta unidos por OR ("horario | loja"), no formato do to_tsquery. So letras e
    /// digitos entram, entao nenhum operador de tsquery vindo do usuario passa. Devolve null quando
    /// nao sobra termo.
    /// </summary>
    public static string? BuildOrQuery(string? queryText)
    {
        var terms = TermPattern.Matches(Normalize(queryText))
            .Select(m => m.Value)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTerms)
            .ToList();

        return terms.Count == 0 ? null : string.Join(" | ", terms);
    }
}
