using AvaBot.DTO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AvaBot.Calibration.Report;

/// <summary>
/// Indicadores e sinais de atencao objetivos de uma mensagem (FR-013, research R9).
/// Nada aqui interpreta a qualidade da resposta: so conta e detecta padroes.
/// </summary>
public class CalibrationSignals
{
    public const string QueryToolName = "consultar_bi";

    private static readonly Regex AsksUserPattern = new(
        @"me confirme|me confirma|me diga|me informe|poderia informar|pode informar|pode confirmar|poderia confirmar|\bqual\b[^.?!\n]*\bvocê\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public int Rounds { get; private init; }
    public int Queries { get; private init; }
    public int QueriesWithError { get; private init; }
    public int RepeatedQueries { get; private init; }
    public bool LimitReached { get; private init; }
    public int? MaxQueryAttempts { get; private init; }
    public int? InputTokens { get; private init; }
    public int? OutputTokens { get; private init; }
    public int? MostExpensiveRound { get; private init; }
    public int? MostExpensiveRoundInputTokens { get; private init; }
    public long ModelDurationMs { get; private init; }
    public long ToolDurationMs { get; private init; }
    public long TotalDurationMs { get; private init; }
    public List<string> Warnings { get; private init; } = new();

    public static CalibrationSignals From(CalibrationTurn turn)
    {
        var result = turn.Result;
        var rounds = result?.Trace.Rounds ?? new List<AgentTestTraceRoundInfo>();
        var warnings = new List<string>();

        var queryCalls = rounds
            .SelectMany(r => r.ToolCalls.Select(c => (Round: r.Number, Call: c)))
            .Where(x => x.Call.Name == QueryToolName)
            .ToList();

        var errorRounds = queryCalls.Where(x => IsError(x.Call.Result)).Select(x => x.Round).Distinct().ToList();
        var withRows = queryCalls.Count(x => RowCount(x.Call.Result) > 0);
        var limitReached = rounds.SelectMany(r => r.ToolCalls)
            .Any(c => c.Result?.Contains("\"attempt_limit\"", StringComparison.Ordinal) == true);

        // Mesma DAX (espacos normalizados) enviada mais de uma vez pelo modelo.
        var repeated = queryCalls
            .Select(x => (x.Round, Dax: NormalizeDax(ReadArgument(x.Call.ArgumentsJson, "dax"))))
            .Where(x => x.Dax.Length > 0)
            .GroupBy(x => x.Dax)
            .Where(g => g.Count() > 1)
            .ToList();

        var tokenRounds = rounds.Where(r => r.InputTokens.HasValue).ToList();
        var mostExpensive = tokenRounds.OrderByDescending(r => r.InputTokens).FirstOrDefault();

        if (turn.Status == CalibrationTurnStatus.Concluida && AsksUser(result?.AssistantResponse))
            warnings.Add("A resposta final pede informação ao usuário.");

        if (errorRounds.Count > 0)
            warnings.Add($"Consulta com erro na(s) rodada(s) {string.Join(", ", errorRounds)}.");

        if (queryCalls.Count > 0 && withRows == 0)
            warnings.Add("Nenhuma consulta ao BI retornou linhas.");

        if (limitReached)
            warnings.Add("O limite de consultas foi atingido.");

        foreach (var group in repeated)
            warnings.Add($"Consulta repetida nas rodadas {string.Join(", ", group.Select(x => x.Round))}.");

        if (result != null && !result.PowerBIAvailable)
            warnings.Add("Ferramentas de BI não estavam disponíveis para este agente.");

        var executorWarnings = rounds
            .Where(r => r.ToolCalls.Any(c => HasWarning(c.Result)))
            .Select(r => r.Number)
            .ToList();
        if (executorWarnings.Count > 0)
            warnings.Add($"Aviso do executor (linhas idênticas) na(s) rodada(s) {string.Join(", ", executorWarnings)}.");

        if (!string.IsNullOrWhiteSpace(result?.Trace.Error))
            warnings.Add("O fluxo foi interrompido por erro antes da resposta final.");

        return new CalibrationSignals
        {
            Rounds = rounds.Count,
            Queries = queryCalls.Count,
            QueriesWithError = queryCalls.Count(x => IsError(x.Call.Result)),
            RepeatedQueries = repeated.Sum(g => g.Count() - 1),
            LimitReached = limitReached,
            MaxQueryAttempts = result?.MaxQueryAttempts,
            InputTokens = tokenRounds.Count > 0 ? tokenRounds.Sum(r => r.InputTokens!.Value) : null,
            OutputTokens = rounds.Any(r => r.OutputTokens.HasValue) ? rounds.Sum(r => r.OutputTokens ?? 0) : null,
            MostExpensiveRound = mostExpensive?.Number,
            MostExpensiveRoundInputTokens = mostExpensive?.InputTokens,
            ModelDurationMs = rounds.Sum(r => r.DurationMs),
            ToolDurationMs = rounds.SelectMany(r => r.ToolCalls).Sum(c => c.DurationMs ?? 0),
            TotalDurationMs = turn.ClientDurationMs,
            Warnings = warnings
        };
    }

    public static bool AsksUser(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return false;

        return response.TrimEnd().EndsWith('?') || AsksUserPattern.IsMatch(response);
    }

    /// <summary>Resultado de ferramenta com "error" na raiz do JSON.</summary>
    public static bool IsError(string? toolResult)
    {
        var root = TryParse(toolResult);
        return root.HasValue && root.Value.ValueKind == JsonValueKind.Object && root.Value.TryGetProperty("error", out _);
    }

    /// <summary>Resultado de consulta com o campo "warning" do executor (linhas identicas).</summary>
    public static bool HasWarning(string? toolResult)
    {
        var root = TryParse(toolResult);
        return root is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("warning", out _);
    }

    /// <summary>"rowCount" do resultado de consultar_bi; null quando nao e um resultado de linhas.</summary>
    public static int? RowCount(string? toolResult)
    {
        var root = TryParse(toolResult);
        if (root is not { ValueKind: JsonValueKind.Object } value
            || !value.TryGetProperty("rowCount", out var rowCount)
            || rowCount.ValueKind != JsonValueKind.Number)
            return null;

        return rowCount.GetInt32();
    }

    public static bool Truncated(string? toolResult)
    {
        var root = TryParse(toolResult);
        return root is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty("truncated", out var truncated)
            && truncated.ValueKind == JsonValueKind.True;
    }

    public static string ReadArgument(string? argumentsJson, string name)
    {
        var root = TryParse(argumentsJson);
        if (root is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(name, out var argument)
            && argument.ValueKind == JsonValueKind.String)
            return argument.GetString() ?? string.Empty;

        return string.Empty;
    }

    public static string NormalizeDax(string dax) =>
        Regex.Replace(dax.Trim(), @"\s+", " ");

    private static JsonElement? TryParse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
