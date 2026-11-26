namespace AvaBot.Infra.Interfaces.AppServices;

/// <summary>
/// O agente nao tem chave OpenAI cadastrada. Deriva de InvalidOperationException para manter o
/// tratamento atual; o backfill da base de conhecimento (feature 021) a distingue de outras falhas.
/// </summary>
public class MissingOpenAIKeyException : InvalidOperationException
{
    public const string DefaultMessage = "Este agente ainda nao possui uma chave OpenAI configurada.";

    public MissingOpenAIKeyException() : base(DefaultMessage) { }
}
