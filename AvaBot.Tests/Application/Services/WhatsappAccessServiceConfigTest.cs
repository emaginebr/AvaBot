using Moq;
using Xunit;
using AvaBot.Application.Services;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Tests.Application.Services;

public class WhatsappAccessServiceConfigTest
{
    private readonly WhatsappAccessTestContext _ctx = new();
    private const long AgentId = WhatsappAccessTestContext.AgentId;

    [Fact]
    public async Task GetConfigAsync_NewAgent_ShouldBeModeAllWithDefaults_AndCounters()
    {
        var agent = new Agent { AgentId = AgentId, Slug = "suporte" };
        _ctx.Contacts.Setup(c => c.CountByAgentAsync(AgentId)).ReturnsAsync(4);
        _ctx.Requests.Setup(r => r.CountPendingAsync(AgentId)).ReturnsAsync(2);

        var config = await _ctx.Service.GetConfigAsync(agent);

        Assert.Equal(1, config.Mode);
        Assert.Equal(4, config.ContactCount);
        Assert.Equal(2, config.PendingRequestCount);
        foreach (var message in new[] { config.OfferMessage, config.ConfirmationMessage, config.WaitingMessage, config.DeclinedMessage })
        {
            Assert.True(message.IsDefault);
            Assert.Equal(message.DefaultText, message.Text);
        }
        Assert.Equal(WhatsappAccessDefaults.OfferMessage, config.OfferMessage.Text);
        Assert.Equal(WhatsappAccessDefaults.DeclinedMessage, config.DeclinedMessage.Text);
    }

    [Fact]
    public async Task UpdateConfigAsync_ShouldTrimCustomTexts_AndNullRestoresDefault()
    {
        var agent = new Agent { AgentId = AgentId, Slug = "suporte", WhatsappWaitingMessage = "antigo" };

        var config = await _ctx.Service.UpdateConfigAsync(agent, new WhatsappAccessConfigUpdateInfo
        {
            Mode = 2,
            OfferMessage = "  Acesso restrito, quer pedir?  ",
            ConfirmationMessage = null,
            WaitingMessage = null,
            DeclinedMessage = "Ok!"
        });

        Assert.Equal(WhatsappAccessMode.Restricted, agent.WhatsappAccessMode);
        Assert.Equal("Acesso restrito, quer pedir?", agent.WhatsappOfferMessage);
        Assert.Null(agent.WhatsappWaitingMessage);
        Assert.Equal(2, config.Mode);
        Assert.False(config.OfferMessage.IsDefault);
        Assert.Equal("Acesso restrito, quer pedir?", config.OfferMessage.Text);
        Assert.True(config.WaitingMessage.IsDefault);
        Assert.Equal("Ok!", config.DeclinedMessage.Text);
        _ctx.Agents.Verify(a => a.UpdateAsync(agent), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigAsync_ToModeAll_ShouldNotTouchContactsOrRequests()
    {
        var agent = WhatsappAccessTestContext.RestrictedAgent();

        await _ctx.Service.UpdateConfigAsync(agent, new WhatsappAccessConfigUpdateInfo { Mode = 1 });

        Assert.Equal(WhatsappAccessMode.All, agent.WhatsappAccessMode);
        _ctx.Contacts.Verify(c => c.DeleteAsync(It.IsAny<WhatsappContact>()), Times.Never);
        _ctx.Requests.Verify(r => r.UpdateAsync(It.IsAny<WhatsappAccessRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfigAsync_InvalidMode_ShouldFail()
    {
        var agent = WhatsappAccessTestContext.RestrictedAgent();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ctx.Service.UpdateConfigAsync(agent, new WhatsappAccessConfigUpdateInfo { Mode = 3 }));

        Assert.Equal(WhatsappAccessService.InvalidModeMessage, ex.Message);
    }
}
