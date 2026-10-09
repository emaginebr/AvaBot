using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Calibration.Local;

// Substitutos em memoria dos repositorios: a calibracao nao le nem grava banco.
// Tudo vem do appsettings.json e nada e persistido.

public class LocalAgentRepository : IAgentRepository<Agent>
{
    private readonly Agent _agent;

    public LocalAgentRepository(Agent agent) => _agent = agent;

    public Task<Agent?> GetByIdAsync(long id) => Task.FromResult(id == _agent.AgentId ? _agent : null);
    public Task<Agent?> GetBySlugAsync(string slug) => Task.FromResult(slug == _agent.Slug ? _agent : null);
    public Task<List<Agent>> GetAllAsync() => Task.FromResult(new List<Agent> { _agent });

    public Task<Agent> CreateAsync(Agent agent) => throw ReadOnly();
    public Task<Agent> UpdateAsync(Agent agent) => throw ReadOnly();
    public Task DeleteAsync(long id) => throw ReadOnly();
    public Task<bool> SlugExistsAsync(string slug, long? excludeId = null) => throw ReadOnly();
    public Task<Agent?> GetByTelegramBotTokenAsync(string token, long? excludeId = null) => throw ReadOnly();
    public Task<Agent?> GetByWhatsappTokenAsync(string token, long? excludeId = null) => throw ReadOnly();

    internal static NotSupportedException ReadOnly() => new("Operacao nao suportada na calibracao local.");
}

public class LocalPowerBIConfigRepository : IAgentPowerBIConfigRepository<AgentPowerBIConfig>
{
    private readonly AgentPowerBIConfig? _config;

    public LocalPowerBIConfigRepository(AgentPowerBIConfig? config) => _config = config;

    public Task<AgentPowerBIConfig?> GetByAgentIdAsync(long agentId) =>
        Task.FromResult(_config != null && _config.AgentId == agentId ? _config : null);

    public Task<AgentPowerBIConfig> UpsertAsync(AgentPowerBIConfig config) => throw LocalAgentRepository.ReadOnly();
}

public class LocalPowerBIDatasetRepository : IPowerBIDatasetRepository<PowerBIDataset>
{
    private readonly List<PowerBIDataset> _datasets;

    public LocalPowerBIDatasetRepository(List<PowerBIDataset> datasets) => _datasets = datasets;

    public Task<List<PowerBIDataset>> GetByAgentIdAsync(long agentId) =>
        Task.FromResult(_datasets.Where(d => d.AgentId == agentId).ToList());

    public Task<PowerBIDataset?> GetByIdAsync(long agentId, long id) =>
        Task.FromResult(_datasets.FirstOrDefault(d => d.AgentId == agentId && d.PowerBIDatasetId == id));

    public Task<bool> ExistsAsync(long agentId, string datasetId, long? excludeId = null) => throw LocalAgentRepository.ReadOnly();
    public Task<bool> ToolKeyExistsAsync(long agentId, string toolKey, long? excludeId = null) => throw LocalAgentRepository.ReadOnly();
    public Task<PowerBIDataset> CreateAsync(PowerBIDataset dataset) => throw LocalAgentRepository.ReadOnly();
    public Task<PowerBIDataset> UpdateAsync(PowerBIDataset dataset) => throw LocalAgentRepository.ReadOnly();
    public Task DeleteAsync(long id) => throw LocalAgentRepository.ReadOnly();
}

/// <summary>O historico de consultas do Power BI nao e gravado: o relatorio ja traz cada execucao.</summary>
public class DiscardQueryLogRepository : IPowerBIQueryLogRepository<PowerBIQueryLog>
{
    public Task<PowerBIQueryLog> CreateAsync(PowerBIQueryLog log) => Task.FromResult(log);
    public Task<(List<PowerBIQueryLog> Items, int Total)> GetPagedByAgentAsync(long agentId, int page, int pageSize) =>
        Task.FromResult((new List<PowerBIQueryLog>(), 0));
    public Task<int> DeleteOlderThanAsync(DateTime cutoff) => Task.FromResult(0);
}

// O teste de agente nao usa sessoes; o ChatService so precisa das dependencias no construtor.
public class UnusedChatSessionRepository : IChatSessionRepository<ChatSession>
{
    public Task<List<ChatSession>> GetByAgentIdAsync(long agentId, int page, int pageSize) => throw LocalAgentRepository.ReadOnly();
    public Task<int> CountByAgentIdAsync(long agentId) => throw LocalAgentRepository.ReadOnly();
    public Task<ChatSession?> GetByIdAsync(long id) => throw LocalAgentRepository.ReadOnly();
    public Task<ChatSession> CreateAsync(ChatSession session) => throw LocalAgentRepository.ReadOnly();
    public Task<ChatSession> UpdateAsync(ChatSession session) => throw LocalAgentRepository.ReadOnly();
    public Task<ChatSession?> GetByResumeTokenAsync(string resumeToken) => throw LocalAgentRepository.ReadOnly();
}

public class UnusedChatMessageRepository : IChatMessageRepository<ChatMessage>
{
    public Task<List<ChatMessage>> GetBySessionIdAsync(long sessionId, int page, int pageSize) => throw LocalAgentRepository.ReadOnly();
    public Task<int> CountBySessionIdAsync(long sessionId) => throw LocalAgentRepository.ReadOnly();
    public Task<List<ChatMessage>> GetRecentBySessionIdAsync(long sessionId, int count) => throw LocalAgentRepository.ReadOnly();
    public Task<ChatMessage> CreateAsync(ChatMessage message) => throw LocalAgentRepository.ReadOnly();
    public Task<List<ChatMessage>> GetLastBySessionIdAsync(long sessionId, int count = 10) => throw LocalAgentRepository.ReadOnly();
}

/// <summary>
/// As chaves vem em texto do appsettings.json local; nao ha cifra a desfazer.
/// </summary>
public class PlainSecretProtector : ISecretProtector
{
    public string Protect(string plain) => plain;
    public string Unprotect(string cipher) => cipher;
}
