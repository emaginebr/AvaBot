using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

public class McpAccessKeyServiceTest
{
    private const long UserId = 7;
    private readonly Mock<IMcpAccessKeyRepository<McpAccessKey>> _repoMock = new();
    private readonly McpAccessKeyService _sut;

    public McpAccessKeyServiceTest()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<McpAccessKey>())).ReturnsAsync((McpAccessKey k) => { k.McpAccessKeyId = 3; return k; });
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<McpAccessKey>())).ReturnsAsync((McpAccessKey k) => k);
        _sut = new McpAccessKeyService(_repoMock.Object, NullLogger<McpAccessKeyService>.Instance);
    }

    private static McpAccessKey StoredKey(string plainKey, int status = 1, int ownerStatus = 1, DateTime? lastUsedAt = null) => new()
    {
        McpAccessKeyId = 3,
        UserId = UserId,
        Name = "Claude",
        KeyHash = McpAccessKey.ComputeHash(plainKey),
        KeyPrefix = McpAccessKey.ExtractPrefix(plainKey),
        Status = status,
        LastUsedAt = lastUsedAt,
        User = new User { UserId = UserId, Name = "Ana", Email = "ana@exemplo.com", Status = ownerStatus }
    };

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ShouldReturnThePlainKeyOnce_AndStoreOnlyHashAndPrefix()
    {
        _repoMock.Setup(r => r.CountActiveByUserAsync(UserId)).ReturnsAsync(0);
        McpAccessKey? stored = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<McpAccessKey>()))
            .Callback<McpAccessKey>(k => stored = k)
            .ReturnsAsync((McpAccessKey k) => k);

        var (key, plainKey) = await _sut.CreateAsync(UserId, "  Claude do notebook ");

        Assert.True(McpAccessKey.HasExpectedFormat(plainKey));
        Assert.NotNull(stored);
        Assert.Equal("Claude do notebook", stored!.Name);
        Assert.Equal(UserId, stored.UserId);
        Assert.Equal(McpAccessKey.ComputeHash(plainKey), stored.KeyHash);
        Assert.Equal(plainKey.Substring("avabot_mcp_".Length, 8), stored.KeyPrefix);
        Assert.Equal(1, stored.Status);
        Assert.Null(stored.LastUsedAt);
        Assert.DoesNotContain(plainKey, stored.KeyHash);
        Assert.Same(stored, key);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenTheAccountAlreadyHasTwentyActiveKeys()
    {
        _repoMock.Setup(r => r.CountActiveByUserAsync(UserId)).ReturnsAsync(20);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(UserId, "Mais uma"));

        Assert.Equal("Limite de 20 chaves ativas por conta. Revogue uma chave antes de criar outra", ex.Message);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<McpAccessKey>()), Times.Never);
    }

    // ---------- AuthenticateAsync ----------

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnTheKeyWithOwner_WhenKeyAndOwnerAreActive()
    {
        var plain = McpAccessKey.GenerateKey();
        _repoMock.Setup(r => r.GetByHashAsync(McpAccessKey.ComputeHash(plain))).ReturnsAsync(StoredKey(plain));

        var key = await _sut.AuthenticateAsync(plain);

        Assert.NotNull(key);
        Assert.Equal(UserId, key!.UserId);
        Assert.Equal("ana@exemplo.com", key.User!.Email);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenTheKeyIsRevoked()
    {
        var plain = McpAccessKey.GenerateKey();
        _repoMock.Setup(r => r.GetByHashAsync(McpAccessKey.ComputeHash(plain))).ReturnsAsync(StoredKey(plain, status: 0));

        Assert.Null(await _sut.AuthenticateAsync(plain));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenTheKeyDoesNotExist()
    {
        var plain = McpAccessKey.GenerateKey();
        _repoMock.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((McpAccessKey?)null);

        Assert.Null(await _sut.AuthenticateAsync(plain));
    }

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiI3In0.assinatura")]
    [InlineData("avabot_mcp_curta")]
    [InlineData("")]
    [InlineData(null)]
    public async Task AuthenticateAsync_ShouldReturnNull_WithoutQueryingTheDatabase_WhenTheFormatIsWrong(string? value)
    {
        Assert.Null(await _sut.AuthenticateAsync(value));
        _repoMock.Verify(r => r.GetByHashAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenTheOwnerIsInactive()
    {
        var plain = McpAccessKey.GenerateKey();
        _repoMock.Setup(r => r.GetByHashAsync(McpAccessKey.ComputeHash(plain))).ReturnsAsync(StoredKey(plain, ownerStatus: 0));

        Assert.Null(await _sut.AuthenticateAsync(plain));
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldTouchLastUsedAt_WhenNullOrOlderThanOneMinute()
    {
        var plain = McpAccessKey.GenerateKey();
        var stored = StoredKey(plain, lastUsedAt: DateTime.UtcNow.AddMinutes(-5));
        _repoMock.Setup(r => r.GetByHashAsync(McpAccessKey.ComputeHash(plain))).ReturnsAsync(stored);

        await _sut.AuthenticateAsync(plain);

        Assert.NotNull(stored.LastUsedAt);
        Assert.True(DateTime.UtcNow - stored.LastUsedAt!.Value < TimeSpan.FromSeconds(5));
        _repoMock.Verify(r => r.UpdateAsync(stored), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldNotTouchLastUsedAt_WhenUsedLessThanOneMinuteAgo()
    {
        var plain = McpAccessKey.GenerateKey();
        var recent = DateTime.UtcNow.AddSeconds(-10);
        var stored = StoredKey(plain, lastUsedAt: recent);
        _repoMock.Setup(r => r.GetByHashAsync(McpAccessKey.ComputeHash(plain))).ReturnsAsync(stored);

        await _sut.AuthenticateAsync(plain);

        Assert.Equal(recent, stored.LastUsedAt);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<McpAccessKey>()), Times.Never);
    }

    // ---------- RevokeAsync ----------

    [Fact]
    public async Task RevokeAsync_ShouldThrowNotFound_WhenTheKeyBelongsToAnotherUser()
    {
        _repoMock.Setup(r => r.GetByIdAsync(3, UserId)).ReturnsAsync((McpAccessKey?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.RevokeAsync(UserId, 3));

        Assert.Equal("Chave nao encontrada", ex.Message);
    }

    [Fact]
    public async Task RevokeAsync_ShouldSetStatusZeroAndRevokedAt_AndBeIdempotent()
    {
        var stored = StoredKey(McpAccessKey.GenerateKey());
        _repoMock.Setup(r => r.GetByIdAsync(3, UserId)).ReturnsAsync(stored);

        var revoked = await _sut.RevokeAsync(UserId, 3);
        Assert.Equal(0, revoked.Status);
        Assert.NotNull(revoked.RevokedAt);
        var firstRevokedAt = revoked.RevokedAt;

        var again = await _sut.RevokeAsync(UserId, 3);
        Assert.Equal(0, again.Status);
        Assert.Equal(firstRevokedAt, again.RevokedAt);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<McpAccessKey>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnTheRepositoryList()
    {
        _repoMock.Setup(r => r.GetAllByUserAsync(UserId)).ReturnsAsync([StoredKey(McpAccessKey.GenerateKey())]);

        var list = await _sut.ListAsync(UserId);

        Assert.Single(list);
    }
}
