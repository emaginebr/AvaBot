using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

public class UserBootstrapServiceTest
{
    private readonly Mock<IUserRepository<User>> _usersMock = new();
    private readonly Mock<IAgentRepository<Agent>> _agentsMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();

    public UserBootstrapServiceTest()
    {
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hash:{p}");
        _usersMock.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => { u.UserId = 1; return u; });
    }

    private UserBootstrapService CreateSut(string? username, string? password)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Username"] = username,
                ["Auth:Password"] = password
            })
            .Build();

        return new UserBootstrapService(_usersMock.Object, _agentsMock.Object, _hasherMock.Object, config, NullLogger<UserBootstrapService>.Instance);
    }

    private void ArrangeState(int users, int orphansBefore, int orphansAfter)
    {
        _usersMock.Setup(r => r.CountAsync()).ReturnsAsync(users);
        _agentsMock.Setup(r => r.AssignOwnerToOrphansAsync(It.IsAny<long>())).ReturnsAsync(orphansBefore);
        _agentsMock.Setup(r => r.CountWithoutOwnerAsync()).ReturnsAsync(orphansAfter);
    }

    [Fact]
    public async Task EnsureAdminAccountAsync_ShouldCreateTheAdmin_AndAssignTheOrphans()
    {
        ArrangeState(users: 0, orphansBefore: 3, orphansAfter: 0);
        User? created = null;
        _usersMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => created = u)
            .ReturnsAsync((User u) => { u.UserId = 1; return u; });

        await CreateSut(" Admin@Empresa.com ", "senha-do-admin").EnsureAdminAccountAsync();

        Assert.NotNull(created);
        Assert.Equal("admin@empresa.com", created!.Email);
        Assert.Equal("Admin@Empresa.com", created.Name);
        Assert.Equal("hash:senha-do-admin", created.PasswordHash);
        Assert.Equal(1, created.Status);
        _agentsMock.Verify(r => r.AssignOwnerToOrphansAsync(1), Times.Once);
    }

    [Fact]
    public async Task EnsureAdminAccountAsync_ShouldCreateTheAdmin_EvenWithoutAgents()
    {
        ArrangeState(users: 0, orphansBefore: 0, orphansAfter: 0);

        await CreateSut("admin", "senha-do-admin").EnsureAdminAccountAsync();

        _usersMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task EnsureAdminAccountAsync_ShouldDoNothing_WhenUsersAlreadyExist()
    {
        ArrangeState(users: 2, orphansBefore: 0, orphansAfter: 0);

        await CreateSut("admin", "senha-do-admin").EnsureAdminAccountAsync();

        _usersMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        _agentsMock.Verify(r => r.AssignOwnerToOrphansAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAdminAccountAsync_ShouldThrow_WhenThereAreOrphansAndNoCredentials()
    {
        ArrangeState(users: 0, orphansBefore: 0, orphansAfter: 4);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut(null, null).EnsureAdminAccountAsync());

        Assert.Contains("4 agente(s) sem dono", ex.Message);
        _usersMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAdminAccountAsync_ShouldThrow_WhenUsersExistButOrphansRemain()
    {
        ArrangeState(users: 1, orphansBefore: 0, orphansAfter: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut("admin", "senha").EnsureAdminAccountAsync());
    }

    [Fact]
    public async Task EnsureAdminAccountAsync_ShouldNotCreateAnything_WhenThereAreNoUsersNoAgentsAndNoCredentials()
    {
        ArrangeState(users: 0, orphansBefore: 0, orphansAfter: 0);

        await CreateSut("", "").EnsureAdminAccountAsync();

        _usersMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }
}
