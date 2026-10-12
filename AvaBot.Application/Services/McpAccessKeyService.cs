using Microsoft.Extensions.Logging;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

/// <summary>
/// Chaves de acesso para assistentes (feature 017). O valor em claro e devolvido uma unica vez
/// por CreateAsync e nunca e logado; o banco guarda so o hash e um prefixo de exibicao.
/// </summary>
public class McpAccessKeyService
{
    public const int MaxActiveKeysPerUser = 20;
    public const string LimitMessage = "Limite de 20 chaves ativas por conta. Revogue uma chave antes de criar outra";
    public const string NotFoundMessage = "Chave nao encontrada";
    private static readonly TimeSpan LastUsedTouchInterval = TimeSpan.FromMinutes(1);

    private readonly IMcpAccessKeyRepository<McpAccessKey> _repository;
    private readonly ILogger<McpAccessKeyService> _logger;

    public McpAccessKeyService(IMcpAccessKeyRepository<McpAccessKey> repository, ILogger<McpAccessKeyService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(McpAccessKey Key, string PlainKey)> CreateAsync(long userId, string name)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (await _repository.CountActiveByUserAsync(userId) >= MaxActiveKeysPerUser)
            throw new InvalidOperationException(LimitMessage);

        var plainKey = McpAccessKey.GenerateKey();
        var key = new McpAccessKey
        {
            UserId = userId,
            Name = trimmed,
            KeyHash = McpAccessKey.ComputeHash(plainKey),
            KeyPrefix = McpAccessKey.ExtractPrefix(plainKey),
            Status = 1
        };

        var created = await _repository.CreateAsync(key);
        _logger.LogInformation("Chave MCP criada: mcpKeyId={KeyId} userId={UserId}", created.McpAccessKeyId, userId);
        return (created, plainKey);
    }

    public async Task<List<McpAccessKey>> ListAsync(long userId)
    {
        return await _repository.GetAllByUserAsync(userId);
    }

    /// <summary>Revoga a chave (status 0). Idempotente; id de outra conta responde como inexistente.</summary>
    public async Task<McpAccessKey> RevokeAsync(long userId, long keyId)
    {
        var key = await _repository.GetByIdAsync(keyId, userId)
            ?? throw new KeyNotFoundException(NotFoundMessage);

        if (!key.IsActive)
            return key;

        key.Status = 0;
        key.RevokedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateAsync(key);
        _logger.LogInformation("Chave MCP revogada: mcpKeyId={KeyId} userId={UserId}", keyId, userId);
        return updated;
    }

    /// <summary>
    /// Devolve a chave (com o dono carregado) quando ela e ativa e o dono e ativo; null para qualquer
    /// outra situacao (formato errado, inexistente, revogada, conta inativa), sem distinguir os casos.
    /// Atualiza o ultimo uso no maximo uma vez por minuto.
    /// </summary>
    public async Task<McpAccessKey?> AuthenticateAsync(string? plainKey)
    {
        if (!McpAccessKey.HasExpectedFormat(plainKey))
            return null;

        var key = await _repository.GetByHashAsync(McpAccessKey.ComputeHash(plainKey!));
        if (key == null || !key.IsActive || key.User == null || !key.User.IsActive)
            return null;

        var now = DateTime.UtcNow;
        if (key.LastUsedAt == null || now - key.LastUsedAt.Value >= LastUsedTouchInterval)
        {
            key.LastUsedAt = now;
            await _repository.UpdateAsync(key);
        }

        return key;
    }
}
