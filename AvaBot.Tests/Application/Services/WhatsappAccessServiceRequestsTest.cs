using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;

namespace AvaBot.Tests.Application.Services;

public class WhatsappAccessServiceRequestsTest
{
    private readonly WhatsappAccessTestContext _ctx = new();
    private readonly Agent _agent = WhatsappAccessTestContext.RestrictedAgent();
    private const long AgentId = WhatsappAccessTestContext.AgentId;
    private const long DeciderId = WhatsappAccessTestContext.OwnerId;

    private WhatsappAccessRequest ArrangeRequest(WhatsappAccessRequestStatus status, string jid = "5511987654321@c.us",
        string? phone = "5511987654321", string? profileName = "Maria")
    {
        var request = new WhatsappAccessRequest
        {
            WhatsappAccessRequestId = 3,
            AgentId = AgentId,
            WhatsappId = jid,
            PhoneNormalized = phone,
            PhoneMatchKey = WhatsappPhone.MatchKey(phone),
            ProfileName = profileName,
            Status = status
        };
        _ctx.Requests.Setup(r => r.GetByIdAsync(3, AgentId)).ReturnsAsync(request);
        return request;
    }

    [Fact]
    public async Task ListRequestsAsync_ShouldPassTheFilter_AndRejectInvalidStatus()
    {
        _ctx.Requests.Setup(r => r.GetByAgentAsync(AgentId, 1)).ReturnsAsync(new List<WhatsappAccessRequest>
        {
            new() { WhatsappAccessRequestId = 3, WhatsappId = "1@lid", PhoneNormalized = null, Status = WhatsappAccessRequestStatus.Pending }
        });

        var list = await _ctx.Service.ListRequestsAsync(_agent, 1);

        var item = Assert.Single(list);
        Assert.Equal(1, item.Status);
        Assert.Null(item.Phone);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _ctx.Service.ListRequestsAsync(_agent, 9));
    }

    [Fact]
    public async Task Approve_ShouldCreateContactFromRequest_WithTheGivenName()
    {
        var request = ArrangeRequest(WhatsappAccessRequestStatus.Pending);

        var contact = await _ctx.Service.ApproveRequestAsync(_agent, 3, "  Maria Silva ", DeciderId);

        var created = Assert.Single(_ctx.CreatedContacts);
        Assert.Equal("Maria Silva", created.Name);
        Assert.Equal(WhatsappContactSource.Approval, created.Source);
        Assert.Equal("5511987654321@c.us", created.WhatsappId);
        Assert.Equal("5511987654321", created.PhoneNormalized);
        Assert.Equal("551187654321", created.PhoneMatchKey);
        Assert.Equal(2, contact.Source);
        Assert.Equal(WhatsappAccessRequestStatus.Approved, request.Status);
        Assert.Equal(DeciderId, request.DecidedByUserId);
        Assert.Equal(_ctx.Time.UtcNow, request.DecidedAt);
        Assert.Equal(created.WhatsappContactId, request.WhatsappContactId);
    }

    [Theory]
    [InlineData("Maria", "5511987654321", "Maria")]
    [InlineData(null, "5511987654321", "5511987654321")]
    [InlineData(null, null, "998877@lid")]
    public async Task Approve_ShouldFallBackToProfileName_ThenPhone_ThenJid(string? profileName, string? phone, string expected)
    {
        ArrangeRequest(WhatsappAccessRequestStatus.Pending, phone == null ? "998877@lid" : "5511987654321@c.us", phone, profileName);

        await _ctx.Service.ApproveRequestAsync(_agent, 3, null, DeciderId);

        Assert.Equal(expected, Assert.Single(_ctx.CreatedContacts).Name);
    }

    [Fact]
    public async Task Approve_LidOnlyRequest_ShouldCreateContactWithoutPhone()
    {
        ArrangeRequest(WhatsappAccessRequestStatus.Pending, "998877@lid", null, "Maria");

        var contact = await _ctx.Service.ApproveRequestAsync(_agent, 3, null, DeciderId);

        Assert.Null(contact.Phone);
        Assert.Null(contact.PhoneNormalized);
        Assert.Equal("998877@lid", contact.WhatsappId);
    }

    [Fact]
    public async Task Approve_ShouldLinkExistingContact_WithoutDuplicating()
    {
        var request = ArrangeRequest(WhatsappAccessRequestStatus.Pending);
        var existing = new WhatsappContact { WhatsappContactId = 40, AgentId = AgentId, Name = "Ana", PhoneMatchKey = "551187654321" };
        _ctx.Contacts.Setup(c => c.FindBySenderAsync(AgentId, "551187654321", "5511987654321@c.us")).ReturnsAsync(existing);

        var contact = await _ctx.Service.ApproveRequestAsync(_agent, 3, null, DeciderId);

        Assert.Empty(_ctx.CreatedContacts);
        Assert.Equal(40, contact.WhatsappContactId);
        Assert.Equal("Ana", existing.Name);
        Assert.Equal("5511987654321@c.us", existing.WhatsappId);
        Assert.Equal(40, request.WhatsappContactId);
    }

    [Fact]
    public async Task Approve_RejectedRequest_ShouldWork()
    {
        var request = ArrangeRequest(WhatsappAccessRequestStatus.Rejected);

        await _ctx.Service.ApproveRequestAsync(_agent, 3, null, DeciderId);

        Assert.Equal(WhatsappAccessRequestStatus.Approved, request.Status);
    }

    [Fact]
    public async Task Approve_AlreadyApproved_ShouldFail()
    {
        ArrangeRequest(WhatsappAccessRequestStatus.Approved);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _ctx.Service.ApproveRequestAsync(_agent, 3, null, DeciderId));

        Assert.Equal(WhatsappAccessService.RequestAlreadyApprovedMessage, ex.Message);
    }

    [Fact]
    public async Task Reject_Pending_ShouldRecordTheDecision()
    {
        var request = ArrangeRequest(WhatsappAccessRequestStatus.Pending);

        var info = await _ctx.Service.RejectRequestAsync(_agent, 3, DeciderId);

        Assert.Equal(WhatsappAccessRequestStatus.Rejected, request.Status);
        Assert.Equal(DeciderId, request.DecidedByUserId);
        Assert.Equal(3, info.Status);
        Assert.Empty(_ctx.CreatedContacts);
    }

    [Theory]
    [InlineData(WhatsappAccessRequestStatus.Approved)]
    [InlineData(WhatsappAccessRequestStatus.Rejected)]
    public async Task Reject_NotPending_ShouldFail(WhatsappAccessRequestStatus status)
    {
        ArrangeRequest(status);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _ctx.Service.RejectRequestAsync(_agent, 3, DeciderId));

        Assert.Equal(WhatsappAccessService.RequestNotPendingMessage, ex.Message);
    }

    [Fact]
    public async Task RequestOfAnotherAgent_ShouldBeNotFound()
    {
        var approve = await Assert.ThrowsAsync<KeyNotFoundException>(() => _ctx.Service.ApproveRequestAsync(_agent, 99, null, DeciderId));
        var reject = await Assert.ThrowsAsync<KeyNotFoundException>(() => _ctx.Service.RejectRequestAsync(_agent, 99, DeciderId));

        Assert.Equal(WhatsappAccessService.RequestNotFoundMessage, approve.Message);
        Assert.Equal(WhatsappAccessService.RequestNotFoundMessage, reject.Message);
    }
}
