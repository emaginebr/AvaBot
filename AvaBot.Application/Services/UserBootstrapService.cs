using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Application.Services;

/// <summary>
/// Migracao de dados da feature 016 (research R5): na primeira subida sem usuarios,
/// cria a conta do administrador a partir de Auth:Username/Auth:Password e atribui a ela
/// os agentes sem dono. Se ainda restar agente sem dono, a API nao sobe.
/// </summary>
public class UserBootstrapService
{
    private readonly IUserRepository<User> _users;
    private readonly IAgentRepository<Agent> _agents;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserBootstrapService> _logger;

    public UserBootstrapService(
        IUserRepository<User> users,
        IAgentRepository<Agent> agents,
        IPasswordHasher hasher,
        IConfiguration configuration,
        ILogger<UserBootstrapService> logger)
    {
        _users = users;
        _agents = agents;
        _hasher = hasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureAdminAccountAsync()
    {
        var username = _configuration["Auth:Username"];
        var password = _configuration["Auth:Password"];
        var hasCredentials = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);

        if (await _users.CountAsync() == 0 && hasCredentials)
        {
            var admin = await _users.CreateAsync(new User
            {
                Name = username!.Trim(),
                Email = User.NormalizeEmail(username),
                PasswordHash = _hasher.Hash(password!),
                Status = 1
            });

            var assigned = await _agents.AssignOwnerToOrphansAsync(admin.UserId);
            _logger.LogInformation("Conta do administrador criada (email={Email}) e {Count} agente(s) atribuido(s)", admin.Email, assigned);
        }

        var orphans = await _agents.CountWithoutOwnerAsync();
        if (orphans > 0)
        {
            throw new InvalidOperationException(
                $"Existem {orphans} agente(s) sem dono e nao ha Auth:Username/Auth:Password configurados para criar a conta do administrador. " +
                "Configure as credenciais ou atribua owner_user_id manualmente.");
        }
    }
}
