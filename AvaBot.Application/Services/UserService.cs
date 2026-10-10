using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AvaBot.DTO;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

public class UserService
{
    public const string EmailInUseMessage = "Este e-mail ja esta em uso";
    public const string LockedMessage = "Credenciais invalidas. Muitas tentativas; aguarde 15 minutos";
    public const string WrongCurrentPasswordMessage = "Senha atual incorreta";

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockWindow = TimeSpan.FromMinutes(15);

    private readonly IUserRepository<User> _repository;
    private readonly IPasswordHasher _hasher;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository<User> repository,
        IPasswordHasher hasher,
        IMemoryCache cache,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _hasher = hasher;
        _cache = cache;
        _logger = logger;
    }

    public async Task<User> RegisterAsync(UserRegisterInfo info)
    {
        var email = User.NormalizeEmail(info.Email);

        if (await _repository.EmailExistsAsync(email))
            throw new InvalidOperationException(EmailInUseMessage);

        var user = new User
        {
            Name = info.Name.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(info.Password),
            Status = 1
        };

        var created = await _repository.CreateAsync(user);
        _logger.LogInformation("Conta criada: userId={UserId} email={Email}", created.UserId, created.Email);
        return created;
    }

    /// <summary>
    /// Devolve o usuario em sucesso e null em qualquer falha (e-mail inexistente, senha errada,
    /// conta inativa): o chamador responde sempre a mesma mensagem generica (FR-006).
    /// Lanca InvalidOperationException quando o e-mail esta bloqueado por excesso de falhas (FR-007).
    /// </summary>
    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var normalized = User.NormalizeEmail(email);

        if (_cache.TryGetValue(LockKey(normalized), out _))
            throw new InvalidOperationException(LockedMessage);

        var user = await _repository.GetByEmailAsync(normalized);
        var ok = user != null && user.IsActive && _hasher.Verify(password, user.PasswordHash);

        if (!ok)
        {
            RegisterFailure(normalized);
            return null;
        }

        _cache.Remove(FailKey(normalized));
        _cache.Remove(LockKey(normalized));
        return user;
    }

    public async Task<User?> GetAsync(long userId)
    {
        return await _repository.GetByIdAsync(userId);
    }

    public async Task<User> UpdateNameAsync(long userId, string name)
    {
        var user = await _repository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuario nao encontrado");

        user.Name = name.Trim();
        return await _repository.UpdateAsync(user);
    }

    public async Task ChangePasswordAsync(long userId, string currentPassword, string newPassword)
    {
        var user = await _repository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuario nao encontrado");

        if (!_hasher.Verify(currentPassword, user.PasswordHash))
            throw new InvalidOperationException(WrongCurrentPasswordMessage);

        user.PasswordHash = _hasher.Hash(newPassword);
        await _repository.UpdateAsync(user);
        _logger.LogInformation("Senha alterada: userId={UserId}", userId);
    }

    private void RegisterFailure(string normalizedEmail)
    {
        var key = FailKey(normalizedEmail);
        var failures = _cache.TryGetValue(key, out int current) ? current + 1 : 1;

        if (failures >= MaxFailedAttempts)
        {
            _cache.Set(LockKey(normalizedEmail), true, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = LockWindow });
            _cache.Remove(key);
            _logger.LogWarning("Login bloqueado por {Minutes} min apos {Count} falhas: email={Email}", LockWindow.TotalMinutes, failures, normalizedEmail);
            return;
        }

        _cache.Set(key, failures, new MemoryCacheEntryOptions { SlidingExpiration = LockWindow });
    }

    private static string FailKey(string email) => $"login-fail:{email}";
    private static string LockKey(string email) => $"login-lock:{email}";
}
