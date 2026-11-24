using FluentAssertions;
using Flurl.Http;
using AvaBot.Tests.API.Support;

namespace AvaBot.Tests.API.Controllers;

/// <summary>
/// Controle de acesso do WhatsApp pelo painel (feature 018): configuracao, contatos e pedidos.
/// Cria um agente proprio e o exclui no fim (a exclusao remove contatos, pedidos e estados).
/// </summary>
public class WhatsappAccessControllerTest : TestBase
{
    private AgentResponse _agent = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        var response = await Api("agents").PostJsonAsync(new { name = "WhatsApp Access Test", systemPrompt = "P" });
        _agent = (await response.GetJsonAsync<ApiResult<AgentResponse>>()).Dados!;
    }

    public override async Task DisposeAsync()
    {
        try { await Api($"agents/{_agent.AgentId}").DeleteAsync(); } catch { /* ja removido */ }
        await base.DisposeAsync();
    }

    private string Path(string suffix) => $"whatsapp/{_agent.Slug}/{suffix}";

    [Fact]
    public async Task GetConfig_NewAgent_ShouldBeModeAll_WithDefaultMessages()
    {
        var body = await (await Api(Path("access")).GetAsync()).GetJsonAsync<ApiResult<AccessConfigResponse>>();

        body.Sucesso.Should().BeTrue();
        body.Dados!.Mode.Should().Be(1);
        body.Dados.OfferMessage.IsDefault.Should().BeTrue();
        body.Dados.OfferMessage.Text.Should().Contain("sim");
        body.Dados.ContactCount.Should().Be(0);
        body.Dados.PendingRequestCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateConfig_ShouldPersistModeAndCustomMessage_AndNullRestoresDefault()
    {
        var saved = await (await Api(Path("access")).PutJsonAsync(new
        {
            mode = 2,
            offerMessage = "Acesso restrito. Quer pedir? sim/nao",
            confirmationMessage = (string?)null,
            waitingMessage = (string?)null,
            declinedMessage = (string?)null
        })).GetJsonAsync<ApiResult<AccessConfigResponse>>();

        saved.Dados!.Mode.Should().Be(2);
        saved.Dados.OfferMessage.IsDefault.Should().BeFalse();
        saved.Dados.OfferMessage.Text.Should().Be("Acesso restrito. Quer pedir? sim/nao");

        var restored = await (await Api(Path("access")).PutJsonAsync(new { mode = 1, offerMessage = (string?)null }))
            .GetJsonAsync<ApiResult<AccessConfigResponse>>();
        restored.Dados!.OfferMessage.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateConfig_EmptyMessage_ShouldReturnBadRequest()
    {
        var action = () => Api(Path("access")).PutJsonAsync(new { mode = 2, offerMessage = "  " });

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Contacts_Crud_ShouldNormalize_AndBlockDuplicatesIgnoringTheNinthDigit()
    {
        var added = await (await Api(Path("contacts")).PostJsonAsync(new { name = "Ana", phone = "(11) 98765-4321" }))
            .GetJsonAsync<ApiResult<ContactResponse>>();
        added.Dados!.PhoneNormalized.Should().Be("5511987654321");
        added.Dados.Source.Should().Be(1);

        var duplicate = () => Api(Path("contacts")).PostJsonAsync(new { name = "Outra", phone = "+55 11 8765-4321" });
        var ex = await duplicate.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(400);
        (await ex.Which.GetResponseJsonAsync<ApiResult<object>>()).Mensagem.Should().Be("Este telefone ja esta cadastrado neste agente");

        var updated = await (await Api(Path($"contacts/{added.Dados.WhatsappContactId}")).PutJsonAsync(new { name = "Ana Maria", phone = "11 98765-4321" }))
            .GetJsonAsync<ApiResult<ContactResponse>>();
        updated.Dados!.Name.Should().Be("Ana Maria");

        var list = await (await Api(Path("contacts")).GetAsync()).GetJsonAsync<ApiResult<List<ContactResponse>>>();
        list.Dados.Should().ContainSingle(c => c.WhatsappContactId == added.Dados.WhatsappContactId);

        (await Api(Path($"contacts/{added.Dados.WhatsappContactId}")).DeleteAsync()).StatusCode.Should().Be(200);
        var after = await (await Api(Path("contacts")).GetAsync()).GetJsonAsync<ApiResult<List<ContactResponse>>>();
        after.Dados.Should().BeEmpty();
    }

    [Fact]
    public async Task Contact_InvalidPhone_ShouldReturnBadRequest()
    {
        var action = () => Api(Path("contacts")).PostJsonAsync(new { name = "Ana", phone = "123" });

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Requests_NewAgent_ShouldBeEmpty_AndUnknownRequestIsNotFound()
    {
        var list = await (await Api(Path("access-requests")).SetQueryParam("status", 1).GetAsync())
            .GetJsonAsync<ApiResult<List<AccessRequestResponse>>>();
        list.Dados.Should().BeEmpty();

        var action = () => Api(Path("access-requests/999999999/reject")).PostAsync();
        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UnknownAgent_ShouldReturnNotFound()
    {
        var action = () => Api("whatsapp/agente-inexistente-xyz-99/contacts").GetAsync();

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task WithoutToken_ShouldReturnUnauthorized()
    {
        using var anonymous = new FlurlClient(Settings.BaseUrl);
        var action = () => anonymous.Request(Path("access")).GetAsync();

        var ex = await action.Should().ThrowAsync<FlurlHttpException>();
        ex.Which.StatusCode.Should().Be(401);
    }
}

public class AccessMessageResponse
{
    public string Text { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string DefaultText { get; set; } = string.Empty;
}

public class AccessConfigResponse
{
    public int Mode { get; set; }
    public AccessMessageResponse OfferMessage { get; set; } = new();
    public AccessMessageResponse ConfirmationMessage { get; set; } = new();
    public AccessMessageResponse WaitingMessage { get; set; } = new();
    public AccessMessageResponse DeclinedMessage { get; set; } = new();
    public int ContactCount { get; set; }
    public int PendingRequestCount { get; set; }
}

public class ContactResponse
{
    public long WhatsappContactId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhoneNormalized { get; set; }
    public string? WhatsappId { get; set; }
    public int Source { get; set; }
}

public class AccessRequestResponse
{
    public long WhatsappAccessRequestId { get; set; }
    public string WhatsappId { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ProfileName { get; set; }
    public int Status { get; set; }
}
