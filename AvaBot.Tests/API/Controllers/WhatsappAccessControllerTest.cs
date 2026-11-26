using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Controllers;
using AvaBot.API.Validators;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using AvaBot.Tests.Application.Services;

namespace AvaBot.Tests.API.Controllers;

public class WhatsappAccessControllerTest
{
    private const long OwnerId = WhatsappAccessTestContext.OwnerId;
    private const long AgentId = WhatsappAccessTestContext.AgentId;
    private const string Slug = "suporte";
    private readonly WhatsappAccessTestContext _ctx = new();
    private readonly WhatsappAccessController _sut;

    public WhatsappAccessControllerTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        // O mesmo mock de agentes serve ao AgentService (dono) e ao WhatsappAccessService (UpdateAsync).
        _ctx.Agents.Setup(r => r.GetBySlugAsync(Slug, OwnerId)).ReturnsAsync(WhatsappAccessTestContext.RestrictedAgent());
        var agentService = new AgentService(_ctx.Agents.Object, new Mock<IKnowledgeChunkRepository>().Object, new Mock<ISecretProtector>().Object, mapper);

        _sut = new WhatsappAccessController(agentService, _ctx.Service,
            new WhatsappAccessConfigUpdateInfoValidator(), new WhatsappContactInsertInfoValidator(), new WhatsappAccessRequestApproveInfoValidator())
            .WithUser(OwnerId);
    }

    private static T Body<T>(IActionResult result) where T : class =>
        Assert.IsType<T>(Assert.IsAssignableFrom<ObjectResult>(result).Value);

    [Fact]
    public async Task AgentOfAnotherOwner_ShouldReturnNotFound()
    {
        var result = await _sut.ListContacts("alheio");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Agente nao encontrado", Assert.IsType<Result<object>>(notFound.Value).Mensagem);
    }

    [Fact]
    public async Task AddContact_ShouldReturnOk_WithTheContact()
    {
        var result = await _sut.AddContact(Slug, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11 98765-4321" });

        Assert.IsType<OkObjectResult>(result);
        var body = Body<Result<WhatsappContactInfo>>(result);
        Assert.True(body.Sucesso);
        Assert.Equal("5511987654321", body.Dados!.PhoneNormalized);
    }

    [Fact]
    public async Task AddContact_InvalidPhone_ShouldReturnBadRequest_WithoutCallingTheService()
    {
        var result = await _sut.AddContact(Slug, new WhatsappContactInsertInfo { Name = "Ana", Phone = "12" });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Telefone invalido. Informe DDD e numero, com ou sem codigo do pais", Assert.IsType<Result<object>>(bad.Value).Mensagem);
        Assert.Empty(_ctx.CreatedContacts);
    }

    [Fact]
    public async Task AddContact_Duplicate_ShouldReturnBadRequest()
    {
        _ctx.Contacts.Setup(c => c.ExistsAsync(AgentId, "551187654321", null, null)).ReturnsAsync(true);

        var result = await _sut.AddContact(Slug, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11 98765-4321" });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(WhatsappAccessService.DuplicatePhoneMessage, Assert.IsType<Result<object>>(bad.Value).Mensagem);
    }

    [Fact]
    public async Task UpdateContact_NotFound_ShouldReturn404()
    {
        var result = await _sut.UpdateContact(Slug, 99, new WhatsappContactInsertInfo { Name = "Ana", Phone = "11 98765-4321" });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(WhatsappAccessService.ContactNotFoundMessage, Assert.IsType<Result<object>>(notFound.Value).Mensagem);
    }

    [Fact]
    public async Task UpdateConfig_EmptyMessage_ShouldReturnBadRequest()
    {
        var result = await _sut.UpdateConfig(Slug, new WhatsappAccessConfigUpdateInfo { Mode = 2, OfferMessage = "" });

        Assert.IsType<BadRequestObjectResult>(result);
        _ctx.Agents.Verify(a => a.UpdateAsync(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task GetConfig_ShouldReturnTheConfig()
    {
        var result = await _sut.GetConfig(Slug);

        var body = Body<Result<WhatsappAccessConfigInfo>>(result);
        Assert.Equal(2, body.Dados!.Mode);
    }

    [Fact]
    public async Task ApproveRequest_ShouldUseTheLoggedUserAsDecider()
    {
        var request = new WhatsappAccessRequest { WhatsappAccessRequestId = 3, AgentId = AgentId, WhatsappId = "1@lid", Status = WhatsappAccessRequestStatus.Pending };
        _ctx.Requests.Setup(r => r.GetByIdAsync(3, AgentId)).ReturnsAsync(request);

        var result = await _sut.ApproveRequest(Slug, 3, null);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(OwnerId, request.DecidedByUserId);
    }

    [Fact]
    public async Task RejectRequest_NotPending_ShouldReturnBadRequest()
    {
        _ctx.Requests.Setup(r => r.GetByIdAsync(3, AgentId)).ReturnsAsync(new WhatsappAccessRequest { WhatsappAccessRequestId = 3, AgentId = AgentId, Status = WhatsappAccessRequestStatus.Rejected });

        var result = await _sut.RejectRequest(Slug, 3);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(WhatsappAccessService.RequestNotPendingMessage, Assert.IsType<Result<object>>(bad.Value).Mensagem);
    }
}
