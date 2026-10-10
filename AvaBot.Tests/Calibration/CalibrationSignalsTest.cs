using System.Text.Json;
using AvaBot.Calibration.Report;
using AvaBot.DTO;

namespace AvaBot.Tests.Calibration;

public class CalibrationSignalsTest
{
    private static AgentTestTraceToolCallInfo Query(string dax, string result) => new()
    {
        Name = "consultar_bi",
        ArgumentsJson = "{\"dataset\":\"d\",\"dax\":" + JsonSerializer.Serialize(dax) + "}",
        Result = result,
        DurationMs = 100
    };

    private static CalibrationTurn Turn(string answer, params AgentTestTraceRoundInfo[] rounds) => new()
    {
        Index = 1,
        Question = "q",
        Status = CalibrationTurnStatus.Concluida,
        ClientDurationMs = 5000,
        Result = new AgentTestResultInfo
        {
            AssistantResponse = answer,
            PowerBIAvailable = true,
            MaxQueryAttempts = 5,
            Trace = new AgentTestTraceInfo { Rounds = rounds.ToList() }
        }
    };

    private static AgentTestTraceRoundInfo Round(int number, int? inputTokens, params AgentTestTraceToolCallInfo[] calls) => new()
    {
        Number = number,
        InputTokens = inputTokens,
        OutputTokens = inputTokens.HasValue ? 10 : null,
        DurationMs = 500,
        ToolCalls = calls.ToList()
    };

    [Theory]
    [InlineData("Qual período você quer?", true)]
    [InlineData("Por favor, me confirme o produto.", true)]
    [InlineData("Me diga uma destas opções: A ou B.", true)]
    [InlineData("Qual tilápia você quer considerar no painel.", true)]
    [InlineData("Em 2024 foram exportados 10 mil kg.", false)]
    public void AsksUser_ShouldDetectRequestsForInformation(string answer, bool expected)
    {
        Assert.Equal(expected, CalibrationSignals.AsksUser(answer));
    }

    [Fact]
    public void From_ShouldCountQueriesErrorsRepetitionsAndTokens()
    {
        var turn = Turn("Valor: 10 kg.",
            Round(1, 1000, Query("EVALUATE  'T'", "{\"error\":{\"category\":\"dax_query\"}}")),
            Round(2, 3000, Query("EVALUATE 'T'", "{\"columns\":[],\"rows\":[[1]],\"rowCount\":1,\"truncated\":false}")),
            Round(3, null));

        var signals = CalibrationSignals.From(turn);

        Assert.Equal(3, signals.Rounds);
        Assert.Equal(2, signals.Queries);
        Assert.Equal(1, signals.QueriesWithError);
        Assert.Equal(1, signals.RepeatedQueries);
        Assert.Equal(4000, signals.InputTokens);
        Assert.Equal(20, signals.OutputTokens);
        Assert.Equal(2, signals.MostExpensiveRound);
        Assert.Equal(1500, signals.ModelDurationMs);
        Assert.Equal(200, signals.ToolDurationMs);
        Assert.Contains("Consulta com erro na(s) rodada(s) 1.", signals.Warnings);
        Assert.Contains("Consulta repetida nas rodadas 1, 2.", signals.Warnings);
        Assert.DoesNotContain("Nenhuma consulta ao BI retornou linhas.", signals.Warnings);
    }

    [Fact]
    public void From_ShouldFlagLimitNoRowsAndQuestionToUser()
    {
        var limit = "{\"error\":{\"category\":\"attempt_limit\",\"queryMayBeCorrected\":false}}";
        var turn = Turn("Você pode confirmar o código SH?",
            Round(1, 100, Query("EVALUATE 'A'", "{\"rows\":[],\"rowCount\":0}")),
            Round(2, 100, Query("EVALUATE 'B'", limit)));

        var signals = CalibrationSignals.From(turn);

        Assert.True(signals.LimitReached);
        Assert.Contains("O limite de consultas foi atingido.", signals.Warnings);
        Assert.Contains("Nenhuma consulta ao BI retornou linhas.", signals.Warnings);
        Assert.Contains("A resposta final pede informação ao usuário.", signals.Warnings);
    }

    [Fact]
    public void From_ShouldFlagExecutorWarning()
    {
        var turn = Turn("Ficou estável.",
            Round(1, 100, Query("EVALUATE SUMMARIZECOLUMNS('T'[Ano])", "{\"rows\":[[2022,1],[2023,1]],\"rowCount\":2,\"warning\":\"ATENÇÃO: linhas iguais\"}")));

        var signals = CalibrationSignals.From(turn);

        Assert.Contains("Aviso do executor (linhas idênticas) na(s) rodada(s) 1.", signals.Warnings);
    }

    [Fact]
    public void From_ShouldReportUnavailablePowerBIAndMissingTokens()
    {
        var turn = Turn("Olá!", Round(1, null));
        turn.Result!.PowerBIAvailable = false;

        var signals = CalibrationSignals.From(turn);

        Assert.Null(signals.InputTokens);
        Assert.Null(signals.MostExpensiveRound);
        Assert.Equal("Ferramentas de BI não estavam disponíveis para este agente.", Assert.Single(signals.Warnings));
    }
}
