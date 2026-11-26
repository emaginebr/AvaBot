using AvaBot.Infra.Interfaces.Repository;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AvaBot.API.Mcp;
using AvaBot.API.Mcp.Tools;
using AvaBot.API.Validators;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Tests.Application.Services;

namespace AvaBot.Tests.API.Mcp;

public class WhatsappAccessToolsTest
{
    private const long OwnerId = McpTestContext.UserId;
    private const long AgentId = WhatsappAccessTestContext.AgentId;
    private readonly WhatsappAccessTestContext _ctx = new();
    private readonly McpCaller _caller = new(OwnerId, McpTestContext.KeyId, "Claude");
    private readonly Agent _agent = WhatsappAccessTestContext.RestrictedAgent();
    private readonly WhatsappAccessTools _sut;

    public WhatsappAccessToolsTest()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<AgentProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        _ctx.Agents.Setup(r => r.GetByIdAsync(AgentId, OwnerId)).ReturnsAsync(_agent);
        var agentService = new AgentService(_ctx.Agents.Object, new Mock<IKnowledgeChunkRepository>().Object, new Mock<ISecretProtector>().Object, mapper);

        _sut = new WhatsappAccessTools(agentService, _ctx.Service, new WhatsappAccessConfigUpdateInfoValidator(),
            new WhatsappContactInsertInfoValidator(), new WhatsappAccessRequestApproveInfoValidator(), NullLogger<WhatsappAccessTools>.Instance);
    }

    [Fact]
    public async Task ForeignAgent_ShouldAnswerNotFound()
    {
        var result = await _sut.ListarContatosWhatsappAsync(_caller, 99);

        Assert.True(result.IsError);
        Assert.Equal("Agente nao encontrado", McpTestContext.Text(result));
    }

    [Fact]
    public async Task ObterAcesso_ShouldNotExposeTheWhatsappToken()
    {
        _agent.WhatsappToken = "WPP-TOKEN-SECRETO";

        var result = await _sut.ObterAcessoWhatsappAsync(_caller, AgentId);

        Assert.NotEqual(true, result.IsError);
        var text = McpTestContext.Text(result);
        Assert.Contains("\"mode\":2", text);
        Assert.DoesNotContain("WPP-TOKEN-SECRETO", text);
    }

    [Fact]
    public async Task AtualizarAcesso_ShouldKeepOmittedMessages_AndRestoreOnlyTheListedOnes()
    {
        _agent.WhatsappOfferMessage = "Oferta personalizada";
        _agent.WhatsappWaitingMessage = "Espera personalizada";

        var result = await _sut.AtualizarAcessoWhatsappAsync(_caller, AgentId, 2, null, "Nova confirmacao", null, null, ["oferta"]);

        Assert.NotEqual(true, result.IsError);
        Assert.Null(_agent.WhatsappOfferMessage);
        Assert.Equal("Nova confirmacao", _agent.WhatsappConfirmationMessage);
        Assert.Equal("Espera personalizada", _agent.WhatsappWaitingMessage);
    }

    [Fact]
    public async Task AtualizarAcesso_UnknownMessageName_ShouldFail()
    {
        var result = await _sut.AtualizarAcessoWhatsappAsync(_caller, AgentId, 2, null, null, null, null, ["boas-vindas"]);

        Assert.True(result.IsError);
        Assert.Contains("boas-vindas", McpTestContext.Text(result));
    }

    [Fact]
    public async Task AtualizarAcesso_InvalidMode_ShouldFail_WithoutSaving()
    {
        var result = await _sut.AtualizarAcessoWhatsappAsync(_caller, AgentId, 5, null, null, null, null, null);

        Assert.True(result.IsError);
        _ctx.Agents.Verify(a => a.UpdateAsync(It.IsAny<Agent>()), Times.Never);
    }

    [Fact]
    public async Task AdicionarContato_InvalidPhone_ShouldFail()
    {
        var result = await _sut.AdicionarContatoWhatsappAsync(_caller, AgentId, "Ana", "123");

        Assert.True(result.IsError);
        Assert.Contains("Telefone invalido", McpTestContext.Text(result));
        Assert.Empty(_ctx.CreatedContacts);
    }

    [Fact]
    public async Task ListarSolicitacoes_ShouldTranslateTheTextualStatus()
    {
        _ctx.Requests.Setup(r => r.GetByAgentAsync(AgentId, 1)).ReturnsAsync(new List<WhatsappAccessRequest>());

        var ok = await _sut.ListarSolicitacoesWhatsappAsync(_caller, AgentId, "Pendente");
        var invalid = await _sut.ListarSolicitacoesWhatsappAsync(_caller, AgentId, "aberta");

        Assert.NotEqual(true, ok.IsError);
        _ctx.Requests.Verify(r => r.GetByAgentAsync(AgentId, 1), Times.Once);
        Assert.True(invalid.IsError);
    }

    [Fact]
    public async Task ApproveAndReject_ShouldUseTheCallerAsDecider()
    {
        var pending = new WhatsappAccessRequest { WhatsappAccessRequestId = 3, AgentId = AgentId, WhatsappId = "1@lid", Status = WhatsappAccessRequestStatus.Pending };
        var other = new WhatsappAccessRequest { WhatsappAccessRequestId = 4, AgentId = AgentId, WhatsappId = "2@lid", Status = WhatsappAccessRequestStatus.Pending };
        _ctx.Requests.Setup(r => r.GetByIdAsync(3, AgentId)).ReturnsAsync(pending);
        _ctx.Requests.Setup(r => r.GetByIdAsync(4, AgentId)).ReturnsAsync(other);

        var approved = await _sut.AprovarSolicitacaoWhatsappAsync(_caller, AgentId, 3, "Maria");
        var rejected = await _sut.RejeitarSolicitacaoWhatsappAsync(_caller, AgentId, 4);

        Assert.NotEqual(true, approved.IsError);
        Assert.NotEqual(true, rejected.IsError);
        Assert.Equal(OwnerId, pending.DecidedByUserId);
        Assert.Equal(OwnerId, other.DecidedByUserId);
        Assert.Equal(WhatsappAccessRequestStatus.Rejected, other.Status);
    }

    [Fact]
    public async Task RemoverContato_ShouldConfirm()
    {
        _ctx.Contacts.Setup(c => c.GetByIdAsync(5, AgentId)).ReturnsAsync(new WhatsappContact { WhatsappContactId = 5, AgentId = AgentId, Name = "Ana" });

        var result = await _sut.RemoverContatoWhatsappAsync(_caller, AgentId, 5);

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("Contato removido", McpTestContext.Text(result));
    }
}
