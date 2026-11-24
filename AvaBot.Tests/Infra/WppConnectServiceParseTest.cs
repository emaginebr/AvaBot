using Xunit;
using AvaBot.Infra.AppServices;

namespace AvaBot.Tests.Infra;

/// <summary>Leitura da resposta de status-session do WPP Connect.</summary>
public class WppConnectServiceParseTest
{
    [Fact]
    public void ParseSessionState_WhilePairing_ShouldReturnStatusAndQrCode()
    {
        var state = WppConnectService.ParseSessionState(
            """{"status":"QRCODE","qrcode":"data:image/png;base64,AAA","urlcode":"2@abc","version":"2.10.89"}""");

        Assert.Equal("QRCODE", state.Status);
        Assert.Equal("data:image/png;base64,AAA", state.QrCode);
    }

    [Fact]
    public void ParseSessionState_WithoutClient_ShouldBeClosedWithoutQrCode()
    {
        var state = WppConnectService.ParseSessionState("""{"status":"CLOSED","qrcode":null}""");

        Assert.Equal("CLOSED", state.Status);
        Assert.Null(state.QrCode);
    }

    [Fact]
    public void ParseSessionState_UnexpectedBody_ShouldBeUnknown()
    {
        var state = WppConnectService.ParseSessionState("""{"message":"x"}""");

        Assert.Equal("UNKNOWN", state.Status);
        Assert.Null(state.QrCode);
    }
}
