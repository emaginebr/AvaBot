namespace AvaBot.Infra.Interfaces.Repository;

public interface IPowerBIQueryLogRepository<T> where T : class
{
    Task<T> CreateAsync(T log);
    /// <summary>Pagina do historico, mais recentes primeiro; status e o valor inteiro de PowerBIQueryStatus (a Interfaces nao referencia o Domain) (feature 017, FR-012).</summary>
    Task<(List<T> Items, int Total)> GetPagedByAgentAsync(long agentId, int page, int pageSize, int? status = null);
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
}
