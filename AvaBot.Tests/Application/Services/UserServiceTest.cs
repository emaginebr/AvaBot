using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

public class UserServiceTest
{
    private readonly Mock<IUserRepository<User>> _repoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly UserService _sut;

    public UserServiceTest()
    {
        _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hash:{p}");
        _hasherMock.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string p, string stored) => stored == $"hash:{p}");

        _sut = new UserService(_repoMock.Object, _hasherMock.Object, _cache, NullLogger<UserService>.Instance);
    }

    private static User ActiveUser(string email = "ana@exemplo.com", int status = 1) => new()
    {
        UserId = 7,
        Name = "Ana",
        Email = email,
        PasswordHash = "hash:senha-certa-123",
        Status = status
    };

    // ---------- Cadastro ----------

    [Fact]
    public async Task RegisterAsync_ShouldNormalizeTheEmail_AndStoreAHash()
    {
        User? created = null;
        _repoMock.Setup(r => r.EmailExistsAsync("ana@exemplo.com")).ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => created = u)
            .ReturnsAsync((User u) => u);

        await _sut.RegisterAsync(new UserRegisterInfo { Name = " Ana ", Email = " Ana@Exemplo.com ", Password = "minhasenha123" });

        Assert.NotNull(created);
        Assert.Equal("ana@exemplo.com", created!.Email);
        Assert.Equal("Ana", created.Name);
        Assert.Equal("hash:minhasenha123", created.PasswordHash);
        Assert.NotEqual("minhasenha123", created.PasswordHash);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenTheEmailAlreadyExistsIgnoringCaseAndSpaces()
    {
        _repoMock.Setup(r => r.EmailExistsAsync("ana@exemplo.com")).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RegisterAsync(new UserRegisterInfo { Name = "Ana", Email = "Ana@Exemplo.com ", Password = "minhasenha123" }));

        Assert.Equal("Este e-mail ja esta em uso", ex.Message);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    // ---------- Login ----------

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnTheUser_WhenCredentialsAreRight()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ana@exemplo.com")).ReturnsAsync(ActiveUser());

        var user = await _sut.AuthenticateAsync(" ANA@exemplo.com ", "senha-certa-123");

        Assert.NotNull(user);
        Assert.Equal(7, user!.UserId);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsWrong()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ana@exemplo.com")).ReturnsAsync(ActiveUser());

        Assert.Null(await _sut.AuthenticateAsync("ana@exemplo.com", "errada"));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ninguem@exemplo.com")).ReturnsAsync((User?)null);

        Assert.Null(await _sut.AuthenticateAsync("ninguem@exemplo.com", "qualquer"));
        _hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenTheAccountIsInactive()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ana@exemplo.com")).ReturnsAsync(ActiveUser(status: 0));

        Assert.Null(await _sut.AuthenticateAsync("ana@exemplo.com", "senha-certa-123"));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldLockTheEmail_AfterFiveConsecutiveFailures()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ana@exemplo.com")).ReturnsAsync(ActiveUser());

        for (var i = 0; i < 5; i++)
            Assert.Null(await _sut.AuthenticateAsync("ana@exemplo.com", "errada"));

        // 6a tentativa: bloqueada mesmo com a senha certa, sem consultar o hash
        _hasherMock.Invocations.Clear();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AuthenticateAsync("ana@exemplo.com", "senha-certa-123"));

        Assert.Contains("15 minutos", ex.Message);
        _hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldNotLock_AfterFourFailuresAndOneSuccess()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ana@exemplo.com")).ReturnsAsync(ActiveUser());

        for (var i = 0; i < 4; i++)
            await _sut.AuthenticateAsync("ana@exemplo.com", "errada");

        Assert.NotNull(await _sut.AuthenticateAsync("ana@exemplo.com", "senha-certa-123"));

        // O sucesso zerou o contador: mais 4 falhas ainda nao bloqueiam
        for (var i = 0; i < 4; i++)
            Assert.Null(await _sut.AuthenticateAsync("ana@exemplo.com", "errada"));

        Assert.NotNull(await _sut.AuthenticateAsync("ana@exemplo.com", "senha-certa-123"));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldLockUnknownEmailsToo_WithoutRevealingThem()
    {
        _repoMock.Setup(r => r.GetByEmailAsync("ninguem@exemplo.com")).ReturnsAsync((User?)null);

        for (var i = 0; i < 5; i++)
            await _sut.AuthenticateAsync("ninguem@exemplo.com", "x");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AuthenticateAsync("ninguem@exemplo.com", "x"));
    }

    // ---------- Conta ----------

    [Fact]
    public async Task UpdateNameAsync_ShouldTrimTheName()
    {
        var user = ActiveUser();
        _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var updated = await _sut.UpdateNameAsync(7, "  Ana Souza  ");

        Assert.Equal("Ana Souza", updated.Name);
        _repoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_AndNotSave_WhenCurrentPasswordIsWrong()
    {
        var user = ActiveUser();
        _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ChangePasswordAsync(7, "errada", "nova-senha-123"));

        Assert.Equal("Senha atual incorreta", ex.Message);
        Assert.Equal("hash:senha-certa-123", user.PasswordHash);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldStoreTheNewHash_WhenCurrentPasswordIsRight()
    {
        var user = ActiveUser();
        _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _sut.ChangePasswordAsync(7, "senha-certa-123", "nova-senha-123");

        Assert.Equal("hash:nova-senha-123", user.PasswordHash);
        Assert.True(_hasherMock.Object.Verify("nova-senha-123", user.PasswordHash));
        Assert.False(_hasherMock.Object.Verify("senha-certa-123", user.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrowNotFound_WhenTheUserDoesNotExist()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ChangePasswordAsync(99, "a", "nova-senha-123"));
    }
}
