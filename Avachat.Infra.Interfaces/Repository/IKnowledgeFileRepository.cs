using Avachat.Domain.Models;

namespace Avachat.Infra.Interfaces.Repository;

public interface IKnowledgeFileRepository
{
    Task<List<KnowledgeFile>> GetByAgentIdAsync(long agentId);
    Task<KnowledgeFile?> GetByIdAsync(long id);
    Task<KnowledgeFile> CreateAsync(KnowledgeFile file);
    Task<KnowledgeFile> UpdateAsync(KnowledgeFile file);
    Task DeleteAsync(long id);
}
