using AvaBot.DTO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AvaBot.Calibration.Report;

/// <summary>
/// Gera o relatorio markdown de calibracao (contracts/calibration-report.md).
/// Nada e truncado (SC-001); todo texto livre vai em bloco cercado e passa pela redacao de segredos.
/// </summary>
public static class CalibrationReportBuilder
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // research R7: chave OpenAI, header Bearer e JWT.
    private static readonly Regex[] SecretPatterns =
    {
        new(@"sk-[A-Za-z0-9_\-]{20,}", RegexOptions.Compiled),
        new(@"Bearer\s+[A-Za-z0-9._\-]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"eyJ[A-Za-z0-9._\-]{20,}", RegexOptions.Compiled)
    };

    public const string AnalysisInstructions =
        "Para cada mensagem, identifique a etapa em que a resposta se degradou: base de conhecimento, " +
        "schema, prompt de sistema, DAX gerada, interpretação do resultado ou decisão de perguntar ao usuário. " +
        "Cite a rodada e o trecho. Sugira mudanças concretas em: prompt do agente, descrições do schema no " +
        "painel, base de conhecimento ou regras do PromptAddendum. Separe o que é erro de dados (modelo do " +
        "Power BI) do que é erro de raciocínio do modelo.";

    public static string Build(CalibrationRun run)
    {
        var sb = new StringBuilder();
        var turns = run.Turns.ToList();
        var model = turns.Select(t => t.Result?.ChatModel).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "(desconhecido)";

        sb.Append("# Relatório de calibração — ").Append(run.AgentName).Append(" (`").Append(run.AgentSlug).AppendLine("`)");
        sb.AppendLine();
        sb.Append("- Data: ").AppendLine(run.StartedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append("- Modelo: ").AppendLine(model);
        foreach (var detail in run.Details)
            sb.Append("- ").AppendLine(Redact(detail));
        sb.Append("- Conversas: ").Append(run.Conversations.Count)
          .Append(" · Mensagens: ").Append(turns.Count)
          .Append(" · Duração total: ").AppendLine(Seconds(run.DurationMs));
        sb.AppendLine();

        for (var c = 0; c < run.Conversations.Count; c++)
        {
            var conversation = run.Conversations[c];
            sb.Append("## Conversa ").Append(c + 1).Append(" — ").AppendLine(conversation.Name);
            sb.AppendLine();

            foreach (var turn in conversation.Turns)
                AppendTurn(sb, turn, conversation.Turns.Count);
        }

        AppendConsolidated(sb, run);

        sb.AppendLine("## Instruções para a IA que analisar este relatório");
        sb.AppendLine();
        sb.AppendLine(AnalysisInstructions);

        // AppendLine usa Environment.NewLine (\r\n no Windows) e os blocos usam \n: sai tudo em \n.
        return sb.ToString().Replace("\r\n", "\n");
    }

    // ---------- Mensagem ----------

    private static void AppendTurn(StringBuilder sb, CalibrationTurn turn, int total)
    {
        sb.Append("### Mensagem ").Append(turn.Index).Append(" de ").Append(total);

        if (turn.Status == CalibrationTurnStatus.NaoEnviada)
        {
            sb.Append(" — não enviada (").Append(turn.Reason).AppendLine(")");
            sb.AppendLine();
            sb.AppendLine("**Pergunta**");
            sb.AppendLine(Fence(turn.Question));
            return;
        }

        sb.AppendLine();
        sb.AppendLine();

        var result = turn.Result;

        sb.AppendLine("**Pergunta**");
        sb.AppendLine(Fence(turn.Question));

        sb.AppendLine("**Resposta**");
        sb.AppendLine(result != null && !string.IsNullOrEmpty(result.AssistantResponse)
            ? Fence(result.AssistantResponse)
            : "_(sem resposta)_");
        sb.AppendLine();

        if (turn.Status == CalibrationTurnStatus.Falhou)
        {
            var reason = Regex.Replace(Redact(turn.Reason ?? "erro desconhecido"), @"\s*\r?\n\s*", " ");
            sb.Append("> **Falhou**: ").AppendLine(reason);
            sb.AppendLine();
        }

        var signals = CalibrationSignals.From(turn);
        AppendSummary(sb, signals);

        if (result == null)
        {
            sb.AppendLine("_A API não devolveu dados desta mensagem._");
            sb.AppendLine();
            return;
        }

        AppendKnowledgeBase(sb, result);
        AppendPowerBI(sb, result);
        AppendHistory(sb, turn, result);

        sb.AppendLine("#### 4. Prompt de sistema");
        sb.AppendLine();
        sb.AppendLine(Fence(result.SystemPrompt));

        AppendRounds(sb, result);
    }

    private static void AppendSummary(StringBuilder sb, CalibrationSignals s)
    {
        sb.AppendLine("#### Resumo");
        sb.AppendLine();
        sb.AppendLine("| Indicador | Valor |");
        sb.AppendLine("|---|---|");
        sb.Append("| Rodadas do modelo | ").Append(s.Rounds).AppendLine(" |");
        sb.Append("| Consultas ao BI | ").Append(s.Queries)
          .Append(" (").Append(s.QueriesWithError).Append(" com erro, ")
          .Append(s.RepeatedQueries).AppendLine(" repetidas) |");
        sb.Append("| Limite de consultas atingido | ").Append(s.LimitReached ? "sim" : "não");
        if (s.MaxQueryAttempts.HasValue)
            sb.Append(" (").Append(s.Queries).Append('/').Append(s.MaxQueryAttempts).Append(')');
        sb.AppendLine(" |");
        sb.Append("| Tokens (entrada / saída) | ").Append(Tokens(s.InputTokens)).Append(" / ").Append(Tokens(s.OutputTokens));
        if (s.MostExpensiveRound.HasValue)
            sb.Append(" — rodada mais cara: ").Append(s.MostExpensiveRound)
              .Append(" (").Append(Tokens(s.MostExpensiveRoundInputTokens)).Append(" entrada)");
        sb.AppendLine(" |");
        sb.Append("| Duração | ").Append(Seconds(s.TotalDurationMs))
          .Append(" (modelo ").Append(Seconds(s.ModelDurationMs))
          .Append(" · ferramentas ").Append(Seconds(s.ToolDurationMs)).AppendLine(") |");
        sb.AppendLine();

        sb.AppendLine("**Sinais de atenção**");
        sb.AppendLine();
        if (s.Warnings.Count == 0)
            sb.AppendLine("Nenhum sinal de atenção.");
        else
            foreach (var warning in s.Warnings)
                sb.Append("- ").AppendLine(warning);
        sb.AppendLine();
    }

    private static void AppendKnowledgeBase(StringBuilder sb, AgentTestResultInfo result)
    {
        sb.AppendLine("#### 1. Base de conhecimento");
        sb.AppendLine();
        sb.Append("Texto pesquisado: ").AppendLine(Inline(result.SearchQuery));
        sb.AppendLine();

        if (result.SearchResults.Count == 0)
        {
            sb.AppendLine("Nenhum trecho encontrado.");
            sb.AppendLine();
            return;
        }

        for (var i = 0; i < result.SearchResults.Count; i++)
        {
            sb.Append("Trecho ").Append(i + 1).AppendLine(":");
            sb.AppendLine(Fence(result.SearchResults[i]));
        }
    }

    private static void AppendPowerBI(StringBuilder sb, AgentTestResultInfo result)
    {
        sb.AppendLine("#### 2. Power BI");
        sb.AppendLine();

        if (!result.PowerBIAvailable)
        {
            sb.AppendLine("Ferramentas de BI não disponíveis para este agente.");
            sb.AppendLine();
            return;
        }

        sb.Append("Ferramentas disponíveis: sim · Datasets: ")
          .Append(result.PowerBIDatasets.Count > 0 ? string.Join(", ", result.PowerBIDatasets) : "(nenhum)")
          .Append(" · Limite: ").Append(result.MaxQueryAttempts?.ToString() ?? "?").AppendLine(" consultas");
        sb.AppendLine();

        if (result.PowerBIQueries.Count == 0)
        {
            sb.AppendLine("Nenhuma ferramenta de BI foi executada.");
            sb.AppendLine();
            return;
        }

        // Cada tentativa enviada ao Power BI, inclusive retentativas automaticas da mesma DAX.
        sb.AppendLine("Execuções registradas (inclui retentativas automáticas):");
        sb.AppendLine();
        sb.AppendLine("| # | Ferramenta | Dataset | Status | Linhas | Duração |");
        sb.AppendLine("|---|---|---|---|---|---|");
        for (var i = 0; i < result.PowerBIQueries.Count; i++)
        {
            var q = result.PowerBIQueries[i];
            sb.Append("| ").Append(i + 1)
              .Append(" | ").Append(q.ToolName)
              .Append(" | ").Append(Cell(q.DatasetName ?? "-"))
              .Append(" | ").Append(q.Success ? "sucesso" : "erro")
              .Append(" | ").Append(q.RowCount?.ToString() ?? "-").Append(q.Truncated ? " (truncado)" : "")
              .Append(" | ").Append(q.DurationMs).AppendLine(" ms |");
        }
        sb.AppendLine();
    }

    private static void AppendHistory(StringBuilder sb, CalibrationTurn turn, AgentTestResultInfo result)
    {
        sb.AppendLine("#### 3. Histórico enviado");
        sb.AppendLine();

        if (turn.History.Count == 0)
        {
            sb.AppendLine("Sem histórico.");
            sb.AppendLine();
            return;
        }

        sb.Append(turn.History.Count).Append(" mensagem(ns) enviada(s)");
        if (result.HistoryOmittedCount > 0)
            sb.Append("; ").Append(result.HistoryOmittedCount).Append(" omitida(s) pelo limite de histórico da aplicação");
        sb.AppendLine(".");
        sb.AppendLine();

        foreach (var message in turn.History)
        {
            sb.Append("- `").Append(message.Role).AppendLine("`");
            sb.AppendLine(Fence(message.Content));
        }
    }

    // ---------- Rodadas ----------

    private static void AppendRounds(StringBuilder sb, AgentTestResultInfo result)
    {
        var rounds = result.Trace.Rounds;

        sb.AppendLine("#### 5. Rodadas do modelo");
        sb.AppendLine();

        if (rounds.Count == 0)
        {
            sb.AppendLine("Nenhuma rodada registrada.");
            sb.AppendLine();
        }

        // Em que rodada cada resultado de ferramenta foi mostrado, para referenciar no delta.
        var shownIn = new Dictionary<string, int>();

        for (var i = 0; i < rounds.Count; i++)
        {
            var round = rounds[i];
            var isLast = i == rounds.Count - 1;

            sb.Append("##### Rodada ").Append(round.Number).Append(" — ").Append(string.IsNullOrEmpty(round.FinishReason) ? "sem resposta" : round.FinishReason)
              .Append(" · ").Append(round.DurationMs).Append(" ms · ")
              .Append(Tokens(round.InputTokens)).Append(" / ").Append(Tokens(round.OutputTokens)).Append(" tokens");
            if (round.ToolChoiceNone)
                sb.Append(" · ferramentas bloqueadas (limite atingido)");
            if (isLast)
                sb.Append(" · rodada final");
            sb.AppendLine();
            sb.AppendLine();

            if (isLast)
            {
                sb.AppendLine("**Prompt final enviado ao modelo** (conjunto completo)");
                sb.AppendLine();
                AppendMessages(sb, round.Messages, result.SystemPrompt, shownIn: null);
            }
            else if (i == 0)
            {
                sb.AppendLine("**Mensagens enviadas** (completas na rodada 1)");
                sb.AppendLine();
                AppendMessages(sb, round.Messages, result.SystemPrompt, shownIn: null);
            }
            else
            {
                var previousCount = rounds[i - 1].Messages.Count;
                sb.AppendLine("**Mensagens novas desde a rodada anterior**");
                sb.AppendLine();
                AppendMessages(sb, round.Messages.Skip(previousCount).ToList(), result.SystemPrompt, shownIn);
            }

            AppendDecision(sb, round, shownIn);
        }

        if (!string.IsNullOrWhiteSpace(result.Trace.Error))
        {
            sb.Append("**Erro**: ").Append(Redact(result.Trace.Error!)).AppendLine(" — as rodadas concluídas estão acima.");
            sb.AppendLine();
        }
    }

    private static void AppendMessages(
        StringBuilder sb, List<AgentTestTraceMessageInfo> messages, string systemPrompt, Dictionary<string, int>? shownIn)
    {
        if (messages.Count == 0)
        {
            sb.AppendLine("_(nenhuma)_");
            sb.AppendLine();
            return;
        }

        foreach (var message in messages)
        {
            sb.Append("- `").Append(message.Role).Append('`');

            if (message.Role == "system" && message.Content == systemPrompt)
            {
                sb.AppendLine(" → prompt de sistema (seção 4)");
                continue;
            }

            if (message.ToolCalls is { Count: > 0 })
            {
                sb.Append(" → chamou ").AppendLine(string.Join(", ", message.ToolCalls.Select(c => $"`{c.Name}` ({c.Id})")));
                if (!string.IsNullOrEmpty(message.Content))
                    sb.AppendLine(Fence(message.Content));
                continue;
            }

            if (message.Role == "tool")
            {
                sb.Append(" (").Append(message.ToolCallId).Append(')');

                if (shownIn != null && message.ToolCallId != null && shownIn.TryGetValue(message.ToolCallId, out var roundNumber))
                {
                    sb.Append(" → resultado já mostrado na rodada ").Append(roundNumber).AppendLine();
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine(Fence(message.Content, LanguageOf(message.Content)));
                continue;
            }

            sb.AppendLine();
            sb.AppendLine(Fence(message.Content));
        }

        sb.AppendLine();
    }

    private static void AppendDecision(StringBuilder sb, AgentTestTraceRoundInfo round, Dictionary<string, int> shownIn)
    {
        if (round.ToolCalls.Count == 0)
        {
            sb.AppendLine("**Resposta da IA**");
            sb.AppendLine(string.IsNullOrEmpty(round.ResponseText) ? "_(vazia)_" : Fence(round.ResponseText!));
            sb.AppendLine();
            return;
        }

        sb.Append("**Decisão do modelo**: chamou ")
          .AppendLine(string.Join(", ", round.ToolCalls.Select(c => $"`{c.Name}`")));
        sb.AppendLine();

        if (!string.IsNullOrEmpty(round.ResponseText))
        {
            sb.AppendLine("Texto junto da decisão:");
            sb.AppendLine(Fence(round.ResponseText!));
        }

        foreach (var call in round.ToolCalls)
        {
            var isQuery = call.Name == CalibrationSignals.QueryToolName;
            var isError = CalibrationSignals.IsError(call.Result);
            var rows = CalibrationSignals.RowCount(call.Result);
            var dataset = CalibrationSignals.ReadArgument(call.ArgumentsJson, "dataset");

            sb.Append("**Chamada `").Append(call.Name).Append("`** (").Append(call.Id).Append(')');
            if (dataset.Length > 0)
                sb.Append(" · dataset `").Append(dataset).Append('`');
            sb.Append(" · ").Append(call.DurationMs.HasValue ? $"{call.DurationMs} ms" : "duração não informada");
            if (call.Result != null)
                sb.Append(" · ").Append(isError ? "erro" : "sucesso");
            if (rows.HasValue)
                sb.Append(" · ").Append(rows).Append(" linha(s)").Append(CalibrationSignals.Truncated(call.Result) ? " (truncado)" : "");
            sb.AppendLine();
            sb.AppendLine();

            var dax = isQuery ? CalibrationSignals.ReadArgument(call.ArgumentsJson, "dax") : string.Empty;
            if (dax.Length > 0)
            {
                sb.AppendLine("DAX:");
                sb.AppendLine(Fence(dax, "dax"));
            }
            else
            {
                sb.AppendLine("Argumentos:");
                sb.AppendLine(Fence(call.ArgumentsJson, "json"));
            }

            sb.AppendLine("Resultado devolvido ao modelo:");
            sb.AppendLine(call.Result == null ? "_(não executada)_" : Fence(call.Result, LanguageOf(call.Result)));
            sb.AppendLine();

            if (!string.IsNullOrEmpty(call.Id))
                shownIn[call.Id] = round.Number;
        }
    }

    // ---------- Consolidado ----------

    private static void AppendConsolidated(StringBuilder sb, CalibrationRun run)
    {
        sb.AppendLine("## Consolidado");
        sb.AppendLine();
        sb.AppendLine("| Conversa | Msg | Rodadas | Consultas | Erros | Sinais | Tokens (entrada/saída) | Tempo | Status |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");

        foreach (var conversation in run.Conversations)
        {
            foreach (var turn in conversation.Turns)
            {
                var s = CalibrationSignals.From(turn);
                var status = turn.Status switch
                {
                    CalibrationTurnStatus.Concluida => "concluída",
                    CalibrationTurnStatus.Falhou => "falhou",
                    _ => "não enviada"
                };

                sb.Append("| ").Append(Cell(conversation.Name))
                  .Append(" | ").Append(turn.Index)
                  .Append(" | ").Append(s.Rounds)
                  .Append(" | ").Append(s.Queries)
                  .Append(" | ").Append(s.QueriesWithError)
                  .Append(" | ").Append(turn.Status == CalibrationTurnStatus.NaoEnviada ? 0 : s.Warnings.Count)
                  .Append(" | ").Append(Tokens(s.InputTokens)).Append(" / ").Append(Tokens(s.OutputTokens))
                  .Append(" | ").Append(Seconds(s.TotalDurationMs))
                  .Append(" | ").Append(status).AppendLine(" |");
            }
        }

        sb.AppendLine();
    }

    // ---------- Helpers ----------

    /// <summary>Bloco cercado com cerca maior que qualquer sequencia de crases do conteudo (research R8).</summary>
    public static string Fence(string content, string language = "")
    {
        var text = Redact(content ?? string.Empty);
        var longest = Regex.Matches(text, "`+").Select(m => m.Length).DefaultIfEmpty(0).Max();
        var fence = new string('`', Math.Max(3, longest + 1));

        return $"{fence}{language}\n{text.TrimEnd('\r', '\n')}\n{fence}";
    }

    public static string Redact(string text)
    {
        foreach (var pattern in SecretPatterns)
            text = pattern.Replace(text, "[redacted]");

        return text;
    }

    private static string Inline(string text)
    {
        var value = Redact(text).Replace("\r", " ").Replace("\n", " ");
        var longest = Regex.Matches(value, "`+").Select(m => m.Length).DefaultIfEmpty(0).Max();
        var ticks = new string('`', longest + 1);
        return $"{ticks} {value} {ticks}";
    }

    private static string Cell(string text) =>
        Redact(text).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private static string LanguageOf(string? content)
    {
        var trimmed = content?.TrimStart() ?? string.Empty;
        return trimmed.StartsWith('{') || trimmed.StartsWith('[') ? "json" : string.Empty;
    }

    private static string Tokens(int? value) =>
        value.HasValue ? value.Value.ToString("N0", PtBr) : "não informado";

    private static string Seconds(long milliseconds) =>
        (milliseconds / 1000.0).ToString("0.0", PtBr) + " s";
}
