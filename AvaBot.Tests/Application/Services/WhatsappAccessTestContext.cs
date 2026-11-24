using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;

namespace AvaBot.Tests.Application.Services;

/// <summary>Relogio controlado pelos testes.</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public DateTime UtcNow => Now.UtcDateTime;
    public void Advance(TimeSpan span) => Now = Now.Add(span);
}

/// <summary>
/// Monta o WhatsappAccessService com repositorios mockados (feature 018). Os mocks guardam o que foi gravado
/// para os testes conferirem o estado do remetente, os pedidos e os contatos.
/// </summary>
public sealed class WhatsappAccessTestContext
{
    public const long AgentId = 10;
    public const long OwnerId = 7;

    public Mock<IWhatsappContactRepository<WhatsappContact>> Contacts { get; } = new();
    public Mock<IWhatsappAccessRequestRepository<WhatsappAccessRequest>> Requests { get; } = new();
    public Mock<IWhatsappSenderStateRepository<WhatsappSenderState>> States { get; } = new();
    public Mock<IAgentRepository<Agent>> Agents { get; } = new();
    public FixedTimeProvider Time { get; } = new();
    public IMapper Mapper { get; }
    public WhatsappAccessService Service { get; }

    public List<WhatsappSenderState> SavedStates { get; } = new();
    public List<WhatsappAccessRequest> CreatedRequests { get; } = new();
    public List<WhatsappAccessRequest> UpdatedRequests { get; } = new();
    public List<WhatsappContact> CreatedContacts { get; } = new();
    public List<WhatsappContact> UpdatedContacts { get; } = new();

    public WhatsappAccessTestContext()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<WhatsappAccessProfile>();
        Mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        States.Setup(s => s.UpsertAsync(It.IsAny<WhatsappSenderState>()))
            .ReturnsAsync((WhatsappSenderState s) => { SavedStates.Add(Clone(s)); return s; });
        Requests.Setup(r => r.CreateAsync(It.IsAny<WhatsappAccessRequest>()))
            .ReturnsAsync((WhatsappAccessRequest r) => { r.WhatsappAccessRequestId = 500 + CreatedRequests.Count; CreatedRequests.Add(r); return r; });
        Requests.Setup(r => r.UpdateAsync(It.IsAny<WhatsappAccessRequest>()))
            .ReturnsAsync((WhatsappAccessRequest r) => { UpdatedRequests.Add(r); return r; });
        Contacts.Setup(c => c.CreateAsync(It.IsAny<WhatsappContact>()))
            .ReturnsAsync((WhatsappContact c) => { c.WhatsappContactId = 900 + CreatedContacts.Count; c.CreatedAt = Time.UtcNow; c.UpdatedAt = Time.UtcNow; CreatedContacts.Add(c); return c; });
        Contacts.Setup(c => c.UpdateAsync(It.IsAny<WhatsappContact>()))
            .ReturnsAsync((WhatsappContact c) => { UpdatedContacts.Add(c); return c; });
        Agents.Setup(a => a.UpdateAsync(It.IsAny<Agent>())).ReturnsAsync((Agent a) => a);

        Service = new WhatsappAccessService(Contacts.Object, Requests.Object, States.Object, Agents.Object, Mapper, Time,
            NullLogger<WhatsappAccessService>.Instance);
    }

    public static Agent RestrictedAgent() => new()
    {
        AgentId = AgentId,
        OwnerUserId = OwnerId,
        Name = "Suporte",
        Slug = "suporte",
        WhatsappToken = "token",
        WhatsappAccessMode = WhatsappAccessMode.Restricted
    };

    public static WhatsappSender PrivateSender(string jid = "5511987654321@c.us", string? profileName = "Maria") => new()
    {
        WhatsappId = jid,
        Phone = WhatsappPhone.FromJid(jid),
        ProfileName = profileName,
        IsGroup = false
    };

    public void ArrangeState(string whatsappId, DateTime? lastAutoReplyAt, DateTime? offerSentAt)
    {
        States.Setup(s => s.GetAsync(AgentId, whatsappId)).ReturnsAsync(new WhatsappSenderState
        {
            WhatsappSenderStateId = 1,
            AgentId = AgentId,
            WhatsappId = whatsappId,
            LastAutoReplyAt = lastAutoReplyAt,
            OfferSentAt = offerSentAt
        });
    }

    public void ArrangeCurrentRequest(WhatsappAccessRequest? request)
    {
        Requests.Setup(r => r.GetCurrentForSenderAsync(AgentId, It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(request);
    }

    private static WhatsappSenderState Clone(WhatsappSenderState s) => new()
    {
        WhatsappSenderStateId = s.WhatsappSenderStateId,
        AgentId = s.AgentId,
        WhatsappId = s.WhatsappId,
        LastAutoReplyAt = s.LastAutoReplyAt,
        OfferSentAt = s.OfferSentAt
    };
}
