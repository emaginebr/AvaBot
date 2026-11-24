using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;

namespace AvaBot.Tests.Application.Services;

/// <summary>Tabela de verdade do contracts/whatsapp-webhook-flow.md §3 (feature 018).</summary>
public class WhatsappAccessServiceEvaluateTest
{
    private readonly WhatsappAccessTestContext _ctx = new();
    private readonly Agent _agent = WhatsappAccessTestContext.RestrictedAgent();
    private const long AgentId = WhatsappAccessTestContext.AgentId;
    private const string Jid = "5511987654321@c.us";

    private DateTime Ago(TimeSpan span) => _ctx.Time.UtcNow - span;

    private static void AssertReply(WhatsappAccessDecision decision, string text)
    {
        Assert.Equal(WhatsappAccessDecisionKind.Reply, decision.Kind);
        Assert.Equal(text, decision.Text);
    }

    // ---------- Modo Todos ----------

    [Fact]
    public async Task ModeAll_ShouldAllow_WithoutTouchingRepositories()
    {
        _agent.WhatsappAccessMode = WhatsappAccessMode.All;

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");

        Assert.Equal(WhatsappAccessDecisionKind.Allow, decision.Kind);
        _ctx.Contacts.VerifyNoOtherCalls();
        _ctx.States.VerifyNoOtherCalls();
    }

    // ---------- Casos 1–3: contato autorizado ----------

    [Fact]
    public async Task Case1_ContactByPhone_ShouldAllow_AndBackfillTheJid()
    {
        var contact = new WhatsappContact { WhatsappContactId = 1, AgentId = AgentId, Name = "Ana", PhoneMatchKey = "551187654321" };
        _ctx.Contacts.Setup(c => c.FindBySenderAsync(AgentId, "551187654321", Jid)).ReturnsAsync(contact);

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");

        Assert.Equal(WhatsappAccessDecisionKind.Allow, decision.Kind);
        Assert.Equal(Jid, contact.WhatsappId);
        Assert.Single(_ctx.UpdatedContacts);
        Assert.Empty(_ctx.SavedStates);
    }

    [Fact]
    public async Task Case2_ContactByLid_ShouldAllow_WithoutPhone()
    {
        var sender = WhatsappAccessTestContext.PrivateSender("998877@lid");
        var contact = new WhatsappContact { WhatsappContactId = 1, AgentId = AgentId, Name = "Ana", WhatsappId = "998877@lid" };
        _ctx.Contacts.Setup(c => c.FindBySenderAsync(AgentId, null, "998877@lid")).ReturnsAsync(contact);

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, sender, "oi");

        Assert.Equal(WhatsappAccessDecisionKind.Allow, decision.Kind);
        Assert.Empty(_ctx.UpdatedContacts);
    }

    [Fact]
    public async Task Case3_SenderWithoutNinthDigit_ShouldMatchContactWithIt()
    {
        var sender = WhatsappAccessTestContext.PrivateSender("551187654321@c.us");
        var contact = new WhatsappContact { WhatsappContactId = 1, AgentId = AgentId, Name = "Ana", PhoneNormalized = "5511987654321", PhoneMatchKey = "551187654321", WhatsappId = "5511987654321@c.us" };
        _ctx.Contacts.Setup(c => c.FindBySenderAsync(AgentId, "551187654321", "551187654321@c.us")).ReturnsAsync(contact);

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, sender, "oi");

        Assert.Equal(WhatsappAccessDecisionKind.Allow, decision.Kind);
        // Ja tinha um JID @c.us: nao e sobrescrito por outro @c.us.
        Assert.Equal("5511987654321@c.us", contact.WhatsappId);
    }

    [Fact]
    public async Task ContactWithCUsJid_ShouldBeUpdatedToLid_WhenMessageArrivesAsResolvedLid()
    {
        var sender = new WhatsappSender { WhatsappId = "998877@lid", Phone = "5511987654321" };
        var contact = new WhatsappContact { WhatsappContactId = 1, AgentId = AgentId, Name = "Ana", PhoneMatchKey = "551187654321", WhatsappId = Jid };
        _ctx.Contacts.Setup(c => c.FindBySenderAsync(AgentId, "551187654321", "998877@lid")).ReturnsAsync(contact);

        await _ctx.Service.EvaluateIncomingAsync(_agent, sender, "oi");

        Assert.Equal("998877@lid", contact.WhatsappId);
    }

    // ---------- Caso 4: grupo ----------

    [Fact]
    public async Task Case4_UnauthorizedInGroup_ShouldIgnore_WithoutState()
    {
        var sender = WhatsappAccessTestContext.PrivateSender();
        sender.IsGroup = true;

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, sender, "@bot oi");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
        Assert.Equal("grupo-nao-autorizado", decision.Reason);
        Assert.Empty(_ctx.SavedStates);
        Assert.Empty(_ctx.CreatedRequests);
    }

    // ---------- Casos 5–6: oferta e limite ----------

    [Fact]
    public async Task Case5_FirstMessage_ShouldReplyOffer_AndRecordState()
    {
        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");

        AssertReply(decision, WhatsappAccessDefaults.OfferMessage);
        var state = Assert.Single(_ctx.SavedStates);
        Assert.Equal(_ctx.Time.UtcNow, state.LastAutoReplyAt);
        Assert.Equal(_ctx.Time.UtcNow, state.OfferSentAt);
        Assert.Equal(Jid, state.WhatsappId);
    }

    [Fact]
    public async Task Case6_SecondMessageWithinOneHour_ShouldIgnore()
    {
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromMinutes(10)), Ago(TimeSpan.FromMinutes(10)));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi de novo");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
        Assert.Equal("limite-1h", decision.Reason);
        Assert.Empty(_ctx.SavedStates);
    }

    // ---------- Casos 7–10: resposta a oferta ----------

    [Fact]
    public async Task Case7_Yes_ShouldCreatePendingRequest_AndReplyConfirmation_IgnoringTheLimit()
    {
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromMinutes(5)), Ago(TimeSpan.FromMinutes(5)));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "Sim!");

        AssertReply(decision, WhatsappAccessDefaults.ConfirmationMessage);
        var request = Assert.Single(_ctx.CreatedRequests);
        Assert.Equal(WhatsappAccessRequestStatus.Pending, request.Status);
        Assert.Equal(Jid, request.WhatsappId);
        Assert.Equal("5511987654321", request.PhoneNormalized);
        Assert.Equal("551187654321", request.PhoneMatchKey);
        Assert.Equal("Maria", request.ProfileName);
        Assert.Equal(_ctx.Time.UtcNow, request.RequestedAt);
        var state = Assert.Single(_ctx.SavedStates);
        Assert.Null(state.OfferSentAt);
        Assert.Equal(_ctx.Time.UtcNow, state.LastAutoReplyAt);
    }

    [Fact]
    public async Task Case8_No_ShouldReplyDeclined_WithoutRequest()
    {
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromMinutes(5)), Ago(TimeSpan.FromMinutes(5)));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "não");

        AssertReply(decision, WhatsappAccessDefaults.DeclinedMessage);
        Assert.Empty(_ctx.CreatedRequests);
        var state = Assert.Single(_ctx.SavedStates);
        Assert.Null(state.OfferSentAt);
        Assert.Equal(_ctx.Time.UtcNow, state.LastAutoReplyAt);
    }

    [Fact]
    public async Task Case9_OtherAnswer_ShouldBeIgnoredByTheLimit()
    {
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromMinutes(5)), Ago(TimeSpan.FromMinutes(5)));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "talvez");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
        Assert.Empty(_ctx.CreatedRequests);
    }

    [Fact]
    public async Task Case10_YesAfterTheWindow_ShouldReplyANewOffer()
    {
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromHours(25)), Ago(TimeSpan.FromHours(25)));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "sim");

        AssertReply(decision, WhatsappAccessDefaults.OfferMessage);
        Assert.Empty(_ctx.CreatedRequests);
    }

    [Fact]
    public async Task YesWithoutAnOffer_ShouldReplyOffer()
    {
        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "sim");

        AssertReply(decision, WhatsappAccessDefaults.OfferMessage);
        Assert.Empty(_ctx.CreatedRequests);
    }

    [Fact]
    public async Task DuplicatePendingRequest_ShouldIgnore()
    {
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromMinutes(5)), Ago(TimeSpan.FromMinutes(5)));
        _ctx.Requests.Setup(r => r.CreateAsync(It.IsAny<WhatsappAccessRequest>())).ThrowsAsync(new DbUpdateException("duplicate"));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "sim");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
        Assert.Equal("pedido-duplicado", decision.Reason);
    }

    // ---------- Casos 11–14: pedido vigente ----------

    private static WhatsappAccessRequest Request(WhatsappAccessRequestStatus status) => new()
    {
        WhatsappAccessRequestId = 3, AgentId = AgentId, WhatsappId = Jid, Status = status
    };

    [Fact]
    public async Task Case11_Pending_ShouldReplyWaiting_AfterOneHour()
    {
        _ctx.ArrangeCurrentRequest(Request(WhatsappAccessRequestStatus.Pending));
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromHours(2)), null);

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");

        AssertReply(decision, WhatsappAccessDefaults.WaitingMessage);
        var state = Assert.Single(_ctx.SavedStates);
        Assert.Null(state.OfferSentAt);
    }

    [Fact]
    public async Task Case12_PendingAndYesWithinOneHour_ShouldIgnore_WithoutNewRequest()
    {
        _ctx.ArrangeCurrentRequest(Request(WhatsappAccessRequestStatus.Pending));
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromMinutes(10)), null);

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "sim");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
        Assert.Empty(_ctx.CreatedRequests);
    }

    [Fact]
    public async Task Case13_Rejected_ShouldIgnoreSilently()
    {
        _ctx.ArrangeCurrentRequest(Request(WhatsappAccessRequestStatus.Rejected));

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
        Assert.Equal("rejeitado", decision.Reason);
        Assert.Empty(_ctx.SavedStates);
    }

    [Fact]
    public async Task Case14_ApprovedButContactRemoved_ShouldReplyOffer()
    {
        _ctx.ArrangeCurrentRequest(Request(WhatsappAccessRequestStatus.Approved));
        _ctx.ArrangeState(Jid, Ago(TimeSpan.FromHours(2)), null);

        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");

        AssertReply(decision, WhatsappAccessDefaults.OfferMessage);
    }

    // ---------- Casos 15–16 ----------

    [Fact]
    public async Task Case15_UnsupportedType_ShouldReplyOffer()
    {
        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), null);

        AssertReply(decision, WhatsappAccessDefaults.OfferMessage);
    }

    [Fact]
    public async Task Case16_CustomMessages_ShouldBeUsed()
    {
        _agent.WhatsappOfferMessage = "Acesso restrito. Quer pedir? sim/nao";
        _agent.WhatsappConfirmationMessage = "Pedido registrado!";

        var offer = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "oi");
        AssertReply(offer, "Acesso restrito. Quer pedir? sim/nao");

        _ctx.ArrangeState(Jid, _ctx.Time.UtcNow, _ctx.Time.UtcNow);
        _ctx.Time.Advance(TimeSpan.FromMinutes(1));
        var confirmation = await _ctx.Service.EvaluateIncomingAsync(_agent, WhatsappAccessTestContext.PrivateSender(), "s");
        AssertReply(confirmation, "Pedido registrado!");
    }

    [Fact]
    public async Task SenderWithoutJid_ShouldIgnore()
    {
        var decision = await _ctx.Service.EvaluateIncomingAsync(_agent, new WhatsappSender { WhatsappId = "" }, "oi");

        Assert.Equal(WhatsappAccessDecisionKind.Ignore, decision.Kind);
    }
}
