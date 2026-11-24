namespace AvaBot.Infra.Interfaces.Repository;

public interface IWhatsappContactRepository<T> where T : class
{
    /// <summary>Contatos do agente ordenados por nome.</summary>
    Task<List<T>> GetAllByAgentAsync(long agentId);
    Task<int> CountByAgentAsync(long agentId);
    /// <summary>Contato pelo id, filtrado pelo agente: id de outro agente responde como inexistente.</summary>
    Task<T?> GetByIdAsync(long id, long agentId);
    /// <summary>Contato que corresponde ao remetente pela chave do telefone ou pelo JID do WhatsApp.</summary>
    Task<T?> FindBySenderAsync(long agentId, string? phoneMatchKey, string? whatsappId);
    Task<bool> ExistsAsync(long agentId, string? phoneMatchKey, string? whatsappId, long? excludeId = null);
    Task<T> CreateAsync(T contact);
    Task<T> UpdateAsync(T contact);
    Task DeleteAsync(T contact);
}
