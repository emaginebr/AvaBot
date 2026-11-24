namespace AvaBot.Infra.Interfaces.Repository;

public interface IWhatsappAccessRequestRepository<T> where T : class
{
    /// <summary>Pedidos do agente, mais recentes primeiro; status nulo = todos.</summary>
    Task<List<T>> GetByAgentAsync(long agentId, int? status);
    Task<int> CountPendingAsync(long agentId);
    Task<T?> GetByIdAsync(long id, long agentId);
    /// <summary>Pedido mais recente do remetente (pelo JID ou pela chave do telefone).</summary>
    Task<T?> GetCurrentForSenderAsync(long agentId, string whatsappId, string? phoneMatchKey);
    Task<T?> GetPendingByPhoneAsync(long agentId, string phoneMatchKey);
    Task<T> CreateAsync(T request);
    Task<T> UpdateAsync(T request);
}
