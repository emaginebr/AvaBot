using System.Text.Json;
using AvaBot.Calibration.Report;
using AvaBot.DTO;

namespace AvaBot.Tests.Calibration;

public class CalibrationReportBuilderTest
{
    private const string SystemPrompt = "Você é o agente ABIPESCA.";
    private const string SchemaText = "Dataset: ABIPESCA\nTabela 'public ABIPESCA_COMTRADE'";
    private const string ErrorResult = "{\"error\":{\"category\":\"dax_query\",\"message\":\"Column 'Exportação (Peso Kg)' in table 'DAX' cannot be found\",\"queryMayBeCorrected\":true}}";
    private const string Answer = "Em 2024 o Brasil exportou **12.345 kg** de tilápia.";

    private static AgentTestTraceMessageInfo Msg(string role, string content, string? toolCallId = null,
        List<AgentTestTraceToolCallInfo>? toolCalls = null) =>
        new() { Role = role, Content = content, ToolCallId = toolCallId, ToolCalls = toolCalls };

    private static AgentTestTraceToolCallInfo SchemaCall() => new()
    {
        Id = "call_1", Name = "listar_schema", ArgumentsJson = "{\"dataset\":\"abipesca\"}", Result = SchemaText, DurationMs = 2
    };

    private static AgentTestTraceToolCallInfo QueryCall(string id, string dax, string result) => new()
    {
        Id = id, Name = "consultar_bi",
        ArgumentsJson = "{\"dataset\":\"abipesca\",\"dax\":" + JsonSerializer.Serialize(dax) + "}",
        Result = result, DurationMs = 579
    };

    // Tres rodadas: schema -> consulta com erro -> resposta final.
    internal static AgentTestResultInfo ThreeRoundResult()
    {
        var schemaCall = SchemaCall();
        var queryCall = QueryCall("call_2", "EVALUATE SUMMARIZECOLUMNS('DAX'[Exportação (Peso Kg)])", ErrorResult);

        var round1 = new List<AgentTestTraceMessageInfo> { Msg("system", SystemPrompt), Msg("user", "Qual foi o volume?") };
        var round2 = round1.Concat(new[] { Msg("assistant", "", toolCalls: new() { schemaCall }), Msg("tool", SchemaText, "call_1") }).ToList();
        var round3 = round2.Concat(new[] { Msg("assistant", "", toolCalls: new() { queryCall }), Msg("tool", ErrorResult, "call_2") }).ToList();

        return new AgentTestResultInfo
        {
            SearchQuery = "Qual foi o volume?",
            SearchResults = new() { "Trecho sobre tilápia" },
            SystemPrompt = SystemPrompt,
            AssistantResponse = Answer,
            ChatModel = "gpt-4o",
            PowerBIAvailable = true,
            PowerBIDatasets = new() { "ABIPESCA - Comércio Internacional" },
            MaxQueryAttempts = 5,
            PowerBIQueries = new()
            {
                new() { ToolName = "listar_schema", DatasetName = "ABIPESCA", Success = true, RowCount = 12 },
                new() { ToolName = "consultar_bi", DatasetName = "ABIPESCA", Success = false, DurationMs = 579 }
            },
            Trace = new AgentTestTraceInfo
            {
                Rounds = new()
                {
                    new() { Number = 1, Messages = round1, FinishReason = "tool_calls", ToolCalls = new() { schemaCall }, InputTokens = 1000, OutputTokens = 10, DurationMs = 900 },
                    new() { Number = 2, Messages = round2, FinishReason = "tool_calls", ToolCalls = new() { queryCall }, InputTokens = 4000, OutputTokens = 40, DurationMs = 800 },
                    new() { Number = 3, Messages = round3, FinishReason = "stop", ResponseText = Answer, DurationMs = 700 }
                }
            }
        };
    }

    private static CalibrationRun Run(params CalibrationConversation[] conversations) => new()
    {
        AgentSlug = "abipesca",
        AgentName = "ABIPESCA",
        StartedAt = new DateTime(2026, 10, 8, 18, 0, 0),
        DurationMs = 12_400,
        Details = new() { "Base de conhecimento: agent_input/biia/docs (5 arquivo(s), 40 trecho(s))" },
        Conversations = conversations.ToList()
    };

    private static CalibrationConversation Single(AgentTestResultInfo result) => new()
    {
        Name = "tilapia-2024",
        Turns = new()
        {
            new() { Index = 1, Question = "Qual foi o volume?", Result = result, Status = CalibrationTurnStatus.Concluida, ClientDurationMs = 3000 }
        }
    };

    private static string Between(string text, string start, string end)
    {
        var from = text.IndexOf(start, StringComparison.Ordinal);
        var to = text.IndexOf(end, from + start.Length, StringComparison.Ordinal);
        return text[from..to];
    }

    [Fact]
    public void Build_ShouldRenderTheWholeFlowInOrder()
    {
        var report = CalibrationReportBuilder.Build(Run(Single(ThreeRoundResult())));

        var sections = new[]
        {
            "# Relatório de calibração — ABIPESCA (`abipesca`)",
            "- Base de conhecimento: agent_input/biia/docs",
            "## Conversa 1 — tilapia-2024",
            "**Pergunta**",
            "**Resposta**",
            "#### Resumo",
            "#### 1. Base de conhecimento",
            "#### 2. Power BI",
            "#### 3. Histórico enviado",
            "#### 4. Prompt de sistema",
            "#### 5. Rodadas do modelo",
            "##### Rodada 1",
            "##### Rodada 2",
            "##### Rodada 3",
            "**Prompt final enviado ao modelo**",
            "## Consolidado",
            "## Instruções para a IA que analisar este relatório"
        };

        var positions = sections.Select(s => report.IndexOf(s, StringComparison.Ordinal)).ToList();
        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.OrderBy(p => p), positions);
    }

    [Fact]
    public void Build_ShouldShowFullMessagesInRoundOneAndOnlyTheDeltaAfterwards()
    {
        var report = CalibrationReportBuilder.Build(Run(Single(ThreeRoundResult())));

        var round2 = Between(report, "##### Rodada 2", "##### Rodada 3");
        Assert.Contains("**Mensagens novas desde a rodada anterior**", round2);
        Assert.Contains("resultado já mostrado na rodada 1", round2);
        Assert.DoesNotContain("Qual foi o volume?", round2);

        var round1 = Between(report, "##### Rodada 1", "##### Rodada 2");
        Assert.Contains("**Mensagens enviadas** (completas na rodada 1)", round1);
        Assert.Contains("Qual foi o volume?", round1);
        Assert.Contains(SchemaText, round1);
    }

    [Fact]
    public void Build_ShouldKeepDaxDiagnosticAndFinalPromptIntact()
    {
        var report = CalibrationReportBuilder.Build(Run(Single(ThreeRoundResult())));

        Assert.Contains("```dax\nEVALUATE SUMMARIZECOLUMNS('DAX'[Exportação (Peso Kg)])\n```", report);
        Assert.Contains(ErrorResult, report);

        var final = report[report.IndexOf("**Prompt final enviado ao modelo**", StringComparison.Ordinal)..];
        Assert.Contains("prompt de sistema (seção 4)", final);
        Assert.Contains(SchemaText, final);
        Assert.Contains(ErrorResult, final);
        Assert.Contains("**Resposta da IA**", final);
        Assert.Contains("não informado / não informado tokens", report);
    }

    [Fact]
    public void Build_ShouldRenderSummaryAndSignals()
    {
        var report = CalibrationReportBuilder.Build(Run(Single(ThreeRoundResult())));

        Assert.Contains("| Rodadas do modelo | 3 |", report);
        Assert.Contains("| Consultas ao BI | 1 (1 com erro, 0 repetidas) |", report);
        Assert.Contains("| Limite de consultas atingido | não (1/5) |", report);
        Assert.Contains("rodada mais cara: 2 (4.000 entrada)", report);
        Assert.Contains("- Consulta com erro na(s) rodada(s) 2.", report);
        Assert.Contains("- Nenhuma consulta ao BI retornou linhas.", report);
    }

    [Fact]
    public void Fence_ShouldUseALongerFenceThanTheContent()
    {
        var fenced = CalibrationReportBuilder.Fence("antes\n````\ncodigo\n````\ndepois");

        Assert.StartsWith("`````\n", fenced);
        Assert.EndsWith("\n`````", fenced);
    }

    [Fact]
    public void Build_ShouldRedactSecrets()
    {
        var result = ThreeRoundResult();
        result.SearchResults = new() { "chave sk-abcdefghijklmnopqrstuvwxyz123456 e Bearer abc.def.ghi e eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload" };

        var report = CalibrationReportBuilder.Build(Run(Single(result)));

        Assert.DoesNotContain("sk-abcdefghijklmnopqrstuvwxyz123456", report);
        Assert.DoesNotContain("Bearer abc.def.ghi", report);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", report);
        Assert.Contains("[redacted]", report);
    }

    [Fact]
    public void Build_ShouldSayWhenThereIsNoKnowledgeOrPowerBI()
    {
        var result = ThreeRoundResult();
        result.SearchResults = new();
        result.PowerBIAvailable = false;
        result.PowerBIDatasets = new();

        var report = CalibrationReportBuilder.Build(Run(Single(result)));

        Assert.Contains("Nenhum trecho encontrado.", report);
        Assert.Contains("Ferramentas de BI não disponíveis para este agente.", report);
        Assert.Contains("Sem histórico.", report);
    }

    [Fact]
    public void Build_ShouldGroupConversationsAndShowNotSentTurns()
    {
        var failed = new CalibrationConversation
        {
            Name = "tilapia-esclarecimento",
            Turns = new()
            {
                new()
                {
                    Index = 1, Question = "Qual foi o volume?", Status = CalibrationTurnStatus.Falhou,
                    Reason = "rate limit", ClientDurationMs = 900,
                    Result = new AgentTestResultInfo
                    {
                        SystemPrompt = SystemPrompt,
                        Trace = new AgentTestTraceInfo
                        {
                            Error = "rate limit",
                            Rounds = new() { new() { Number = 1, Messages = new() { Msg("system", SystemPrompt), Msg("user", "Qual foi o volume?") } } }
                        }
                    }
                },
                new() { Index = 2, Question = "Todos os produtos", Status = CalibrationTurnStatus.NaoEnviada, Reason = "falha na mensagem 1" }
            }
        };

        var report = CalibrationReportBuilder.Build(Run(Single(ThreeRoundResult()), failed));

        Assert.Contains("## Conversa 2 — tilapia-esclarecimento", report);
        Assert.Contains("> **Falhou**: rate limit", report);
        Assert.Contains("**Erro**: rate limit — as rodadas concluídas estão acima.", report);
        Assert.Contains("### Mensagem 2 de 2 — não enviada (falha na mensagem 1)", report);
        Assert.Contains("| tilapia-2024 | 1 | 3 | 1 | 1 |", report);
        Assert.Contains("| tilapia-esclarecimento | 2 | 0 | 0 | 0 | 0 |", report);
    }

    [Fact]
    public void Build_ShouldShowTheHistorySentWithTheMessage()
    {
        var result = ThreeRoundResult();
        result.HistoryOmittedCount = 2;
        var conversation = Single(result);
        conversation.Turns[0].History = new()
        {
            new() { Role = "user", Content = "Pergunta anterior" },
            new() { Role = "assistant", Content = "Qual tilápia você quer?" }
        };

        var report = CalibrationReportBuilder.Build(Run(conversation));

        var history = Between(report, "#### 3. Histórico enviado", "#### 4. Prompt de sistema");
        Assert.Contains("2 mensagem(ns) enviada(s); 2 omitida(s)", history);
        Assert.Contains("Qual tilápia você quer?", history);
    }
}
