using AvaBot.DTO;

namespace AvaBot.Application.Services;

/// <summary>
/// Falha do teste de agente depois que o modelo ja foi chamado: carrega o resultado
/// parcial (rodadas concluidas e o erro) para o controller devolver no corpo do 500.
/// </summary>
public class AgentTestFailedException : Exception
{
    public AgentTestFailedException(string message, AgentTestResultInfo partialResult, Exception innerException)
        : base(message, innerException)
    {
        PartialResult = partialResult;
    }

    public AgentTestResultInfo PartialResult { get; }
}
