using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using AvaBot.DTO;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

public class AgentService
{
    private readonly IAgentRepository<Agent> _repository;
    private readonly IElasticsearchService _esService;
    private readonly ISecretProtector _secretProtector;
    private readonly IMapper _mapper;

    public AgentService(
        IAgentRepository<Agent> repository,
        IElasticsearchService esService,
        ISecretProtector secretProtector,
        IMapper mapper)
    {
        _repository = repository;
        _esService = esService;
        _secretProtector = secretProtector;
        _mapper = mapper;
    }

    // Painel: tudo filtrado pelo dono do token (feature 016, contracts/ownership.md).
    public async Task<List<Agent>> GetAllAsync(long ownerUserId)
    {
        return await _repository.GetAllByOwnerAsync(ownerUserId);
    }

    public async Task<Agent?> GetOwnedByIdAsync(long id, long ownerUserId)
    {
        return await _repository.GetByIdAsync(id, ownerUserId);
    }

    public async Task<Agent?> GetOwnedBySlugAsync(string slug, long ownerUserId)
    {
        return await _repository.GetBySlugAsync(slug, ownerUserId);
    }

    // Publico/interno (chat, widget, webhooks, ChatService): sem dono.
    public async Task<Agent?> GetBySlugAsync(string slug)
    {
        return await _repository.GetBySlugAsync(slug);
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Agent> CreateAsync(AgentInsertInfo info, long ownerUserId)
    {
        await ValidateTelegramBotTokenAsync(info.TelegramBotToken);

        var agent = _mapper.Map<Agent>(info);
        agent.Status = 1;
        agent.OwnerUserId = ownerUserId;
        agent.Slug = await GenerateUniqueSlugAsync(info.Name);

        if (!string.IsNullOrEmpty(info.TelegramBotToken))
            agent.TelegramWebhookSecret = TelegramService.GenerateWebhookSecret();

        ApplyOpenAIApiKey(agent, info);

        return await _repository.CreateAsync(agent);
    }

    public async Task<Agent?> UpdateAsync(long id, AgentInsertInfo info, long ownerUserId)
    {
        var agent = await _repository.GetByIdAsync(id, ownerUserId);
        if (agent == null) return null;

        await ValidateTelegramBotTokenAsync(info.TelegramBotToken, id);

        var oldName = agent.Name;
        var hadToken = !string.IsNullOrEmpty(agent.TelegramBotToken);
        _mapper.Map(info, agent);

        if (!string.Equals(oldName, info.Name, StringComparison.Ordinal))
            agent.Slug = await GenerateUniqueSlugAsync(info.Name, id);

        if (!string.IsNullOrEmpty(info.TelegramBotToken) && !hadToken)
            agent.TelegramWebhookSecret = TelegramService.GenerateWebhookSecret();

        ApplyOpenAIApiKey(agent, info);

        return await _repository.UpdateAsync(agent);
    }

    // FR: campo em branco preserva a credencial salva; remocao e sempre explicita (D2).
    private void ApplyOpenAIApiKey(Agent agent, AgentInsertInfo info)
    {
        var newKey = info.OpenAIApiKey?.Trim();
        var hasNewKey = !string.IsNullOrEmpty(newKey);

        if (info.RemoveOpenAIApiKey && hasNewKey)
            throw new InvalidOperationException("Envie a nova chave ou a remocao, nao os dois");

        if (info.RemoveOpenAIApiKey)
        {
            agent.OpenAIApiKeyEncrypted = null;
            return;
        }

        if (hasNewKey)
            agent.OpenAIApiKeyEncrypted = _secretProtector.Protect(newKey!);
    }

    /// <summary>Resolve a credencial salva do agente em texto claro. Somente para uso interno (diagnostico).</summary>
    public async Task<string> GetOpenAIApiKeyAsync(long agentId, long ownerUserId)
    {
        var agent = await _repository.GetByIdAsync(agentId, ownerUserId)
            ?? throw new KeyNotFoundException("Agente nao encontrado");

        if (string.IsNullOrEmpty(agent.OpenAIApiKeyEncrypted))
            throw new InvalidOperationException("Este agente ainda nao tem uma chave OpenAI salva");

        return _secretProtector.Unprotect(agent.OpenAIApiKeyEncrypted);
    }

    private async Task ValidateTelegramBotTokenAsync(string? token, long? excludeId = null)
    {
        if (string.IsNullOrEmpty(token)) return;

        var existing = await _repository.GetByTelegramBotTokenAsync(token, excludeId);
        if (existing != null)
            throw new InvalidOperationException("Este TelegramBotToken ja esta em uso por outro agente");
    }

    private async Task ValidateWhatsappTokenAsync(string? token, long? excludeId = null)
    {
        if (string.IsNullOrEmpty(token)) return;

        var existing = await _repository.GetByWhatsappTokenAsync(token, excludeId);
        if (existing != null)
            throw new InvalidOperationException("Este WhatsappToken ja esta em uso por outro agente");
    }

    public async Task<bool> DeleteAsync(long id, long ownerUserId)
    {
        var agent = await _repository.GetByIdAsync(id, ownerUserId);
        if (agent == null) return false;
        await _esService.DeleteChunksByAgentIdAsync(id);
        await _repository.DeleteAsync(id);
        return true;
    }

    public async Task<Agent?> ToggleStatusAsync(long id, long ownerUserId)
    {
        var agent = await _repository.GetByIdAsync(id, ownerUserId);
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
