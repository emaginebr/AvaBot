namespace AvaBot.Infra.Interfaces.Repository;

public interface IWhatsappSenderStateRepository<T> where T : class
{
    Task<T?> GetAsync(long agentId, string whatsappId);
    /// <summary>Insere ou atualiza pelo par (agente, JID).</summary>
    Task<T> UpsertAsync(T state);
}
