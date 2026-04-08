using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using Avachat.DTO;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.Repository;

namespace Avachat.Application.Services;

public class AgentService
{
    private readonly IAgentRepository<Agent> _repository;
    private readonly IMapper _mapper;

    public AgentService(IAgentRepository<Agent> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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
        var agent = _mapper.Map<Agent>(info);
        agent.Status = 1;
        agent.Slug = await GenerateUniqueSlugAsync(info.Name);
        return await _repository.CreateAsync(agent);
    }

    public async Task<Agent?> UpdateAsync(long id, AgentInsertInfo info)
    {
        var agent = await _repository.GetByIdAsync(id);
        if (agent == null) return null;

        var oldName = agent.Name;
        _mapper.Map(info, agent);

        if (!string.Equals(oldName, info.Name, StringComparison.Ordinal))
            agent.Slug = await GenerateUniqueSlugAsync(info.Name, id);

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

    private async Task<string> GenerateUniqueSlugAsync(string name, long? excludeId = null)
    {
        var slug = Slugify(name);

        if (!await _repository.SlugExistsAsync(slug, excludeId))
            return slug;

        for (int i = 2; ; i++)
        {
            var candidate = $"{slug}-{i}";
            if (!await _repository.SlugExistsAsync(candidate, excludeId))
                return candidate;
        }
    }

    public static string Slugify(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
        result = Regex.Replace(result, @"[\s-]+", "-");
        result = result.Trim('-');

        return string.IsNullOrEmpty(result) ? "agent" : result;
    }
}
