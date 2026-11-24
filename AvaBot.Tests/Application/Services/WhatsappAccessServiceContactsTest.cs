using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Tests.Application.Services;

public class WhatsappAccessServiceContactsTest
{
    private readonly WhatsappAccessTestContext _ctx = new();
    private readonly Agent _agent = WhatsappAccessTestContext.RestrictedAgent();
    private const long AgentId = WhatsappAccessTestContext.AgentId;

    [Fact]
    public async Task AddContactAsync_ShouldStoreTypedNormalizedAndMatchKey_AsManual()
    {
        var info = await _ctx.Service.AddContactAsync(_agent, new WhatsappContactInsertInfo { Name = "  Ana ", Phone = "(11) 98765-4321" });

        var created = Assert.Single(_ctx.CreatedContacts);
        Assert.Equal("Ana", created.Name);
        Assert.Equal("(11) 98765-4321", created.Phone);
        Assert.Equal("5511987654321", created.PhoneNormalized);
        Assert.Equal("551187654321", created.PhoneMatchKey);
        Assert.Equal(WhatsappContactSource.Manual, created.Source);
        Assert.Equal(AgentId, created.AgentId);
        Assert.Equal(1, info.Source);
        Assert.Equal("5511987654321", info.PhoneNormalized);
    }

    [Fact]
    public async Task AddContactAsync_ShouldRejectDuplicate_ByTheNinthDigitKey()
    {
        _ctx.Contacts.Setup(c => c.ExistsAsync(AgentId, "551187654321", null, null)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ctx.Service.AddContactAsync(_agent, new WhatsappContactInsertInfo { Name = "Ana", Phone = "+55 11 8765-4321" }));

        Assert.Equal(WhatsappAccessService.DuplicatePhoneMessage, ex.Message);
        Assert.Empty(_ctx.CreatedContacts);
    }

    [Fact]
    public async Task AddContactAsync_ShouldRejectInvalidPhone()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ctx.Service.AddContactAsync(_agent, new WhatsappContactInsertInfo { Name = "Ana", Phone = "123" }));

        Assert.Equal(WhatsappAccessService.InvalidPhoneMessage, ex.Message);
    }

    [Fact]
    public async Task AddContactAsync_ShouldApprovePendingRequestWithTheSamePhone()
    {
        var pending = new WhatsappAccessRequest { WhatsappAccessRequestId = 3, AgentId = AgentId, WhatsappId = "5511987654321@c.us", PhoneMatchKey = "551187654321", Status = WhatsappAccessRequestStatus.Pending };
        _ctx.Requests.Setup(r => r.GetPendingByPhoneAsync(AgentId, "551187654321")).ReturnsAsync(pending);

        await _ctx.Service.AddContactAsync(_agent, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11987654321" });

        var updated = Assert.Single(_ctx.UpdatedRequests);
        Assert.Equal(WhatsappAccessRequestStatus.Approved, updated.Status);
        Assert.Equal(_ctx.CreatedContacts[0].WhatsappContactId, updated.WhatsappContactId);
        Assert.Equal(_ctx.Time.UtcNow, updated.DecidedAt);
        Assert.Null(updated.DecidedByUserId);
    }

    [Fact]
    public async Task UpdateContactAsync_ShouldThrowNotFound_ForContactOfAnotherAgent()
    {
        _ctx.Contacts.Setup(c => c.GetByIdAsync(5, AgentId)).ReturnsAsync((WhatsappContact?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _ctx.Service.UpdateContactAsync(_agent, 5, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11987654321" }));

        Assert.Equal(WhatsappAccessService.ContactNotFoundMessage, ex.Message);
    }

    [Fact]
    public async Task UpdateContactAsync_ShouldClearCUsJidOfAnotherNumber_AndKeepLid()
    {
        var withCUs = new WhatsappContact { WhatsappContactId = 5, AgentId = AgentId, Name = "Ana", WhatsappId = "5511987654321@c.us" };
        var withLid = new WhatsappContact { WhatsappContactId = 6, AgentId = AgentId, Name = "Bia", WhatsappId = "123456@lid" };
        _ctx.Contacts.Setup(c => c.GetByIdAsync(5, AgentId)).ReturnsAsync(withCUs);
        _ctx.Contacts.Setup(c => c.GetByIdAsync(6, AgentId)).ReturnsAsync(withLid);

        await _ctx.Service.UpdateContactAsync(_agent, 5, new WhatsappContactInsertInfo { Name = "Ana Maria", Phone = "21 99999-0000" });
        await _ctx.Service.UpdateContactAsync(_agent, 6, new WhatsappContactInsertInfo { Name = "Bia", Phone = "21 98888-0000" });

        Assert.Null(withCUs.WhatsappId);
        Assert.Equal("Ana Maria", withCUs.Name);
        Assert.Equal("5521999990000", withCUs.PhoneNormalized);
        Assert.Equal("123456@lid", withLid.WhatsappId);
    }

    [Fact]
    public async Task UpdateContactAsync_ShouldKeepCUsJid_WhenThePhoneIsTheSameNumber()
    {
        var contact = new WhatsappContact { WhatsappContactId = 5, AgentId = AgentId, Name = "Ana", WhatsappId = "551187654321@c.us" };
        _ctx.Contacts.Setup(c => c.GetByIdAsync(5, AgentId)).ReturnsAsync(contact);

        await _ctx.Service.UpdateContactAsync(_agent, 5, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11 98765-4321" });

        Assert.Equal("551187654321@c.us", contact.WhatsappId);
    }

    [Fact]
    public async Task UpdateContactAsync_ShouldRejectDuplicate_ExcludingItself()
    {
        var contact = new WhatsappContact { WhatsappContactId = 5, AgentId = AgentId, Name = "Ana" };
        _ctx.Contacts.Setup(c => c.GetByIdAsync(5, AgentId)).ReturnsAsync(contact);
        _ctx.Contacts.Setup(c => c.ExistsAsync(AgentId, "551187654321", null, 5)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ctx.Service.UpdateContactAsync(_agent, 5, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11987654321" }));

        Assert.Equal(WhatsappAccessService.DuplicatePhoneMessage, ex.Message);
    }

    [Fact]
    public async Task RemoveContactAsync_ShouldDelete_OrThrowNotFound()
    {
        var contact = new WhatsappContact { WhatsappContactId = 5, AgentId = AgentId, Name = "Ana" };
        _ctx.Contacts.Setup(c => c.GetByIdAsync(5, AgentId)).ReturnsAsync(contact);

        await _ctx.Service.RemoveContactAsync(_agent, 5);
        _ctx.Contacts.Verify(c => c.DeleteAsync(contact), Times.Once);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _ctx.Service.RemoveContactAsync(_agent, 99));
    }

    [Fact]
    public async Task ListContactsAsync_ShouldMapAll()
    {
        _ctx.Contacts.Setup(c => c.GetAllByAgentAsync(AgentId)).ReturnsAsync(new List<WhatsappContact>
        {
            new() { WhatsappContactId = 1, Name = "Ana", Phone = "11987654321", PhoneNormalized = "5511987654321", Source = WhatsappContactSource.Manual },
            new() { WhatsappContactId = 2, Name = "Bia", WhatsappId = "1@lid", Source = WhatsappContactSource.Approval }
        });

        var list = await _ctx.Service.ListContactsAsync(_agent);

        Assert.Equal(2, list.Count);
        Assert.Equal(2, list[1].Source);
        Assert.Null(list[1].Phone);
        Assert.Equal("1@lid", list[1].WhatsappId);
    }
}
