using Avachat.Domain.DTOs;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Application.Services;

public class AgentService
{
    private readonly IAgentRepository _repository;

    public AgentService(IAgentRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Agent>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Agent?> GetBySlugAsync(string slug)
    {
        return await _repository.GetBySlugAsync(slug);
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Agent> CreateAsync(AgentInsertInfo info)
    {
        var agent = new Agent
        {
            Name = info.Name,
            Slug = info.Slug,
            Description = info.Description,
            SystemPrompt = info.SystemPrompt,
            CollectName = info.CollectName,
            CollectEmail = info.CollectEmail,
            CollectPhone = info.CollectPhone,
            Status = 1
        };

        return await _repository.CreateAsync(agent);
    }

    public async Task<Agent?> UpdateAsync(long id, AgentInsertInfo info)
    {
        var agent = await _repository.GetByIdAsync(id);
        if (agent == null) return null;

        agent.Name = info.Name;
        agent.Slug = info.Slug;
        agent.Description = info.Description;
        agent.SystemPrompt = info.SystemPrompt;
        agent.CollectName = info.CollectName;
        agent.CollectEmail = info.CollectEmail;
        agent.CollectPhone = info.CollectPhone;

        return await _repository.UpdateAsync(agent);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var agent = await _repository.GetByIdAsync(id);
        if (agent == null) return false;
        await _repository.DeleteAsync(id);
        return true;
    }

    public async Task<Agent?> ToggleStatusAsync(long id)
    {
        var agent = await _repository.GetByIdAsync(id);
        if (agent == null) return null;

        agent.Status = agent.Status == 1 ? 0 : 1;
        return await _repository.UpdateAsync(agent);
    }

    public async Task<bool> SlugExistsAsync(string slug, long? excludeId = null)
    {
        return await _repository.SlugExistsAsync(slug, excludeId);
    }
}
