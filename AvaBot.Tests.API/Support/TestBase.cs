using System.Text.Json;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;

namespace AvaBot.Tests.API.Support;

public abstract class TestBase : IAsyncLifetime
{
    protected readonly ApiSettings Settings;
    protected readonly IFlurlClient Client;

    protected TestBase()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        Settings = new ApiSettings();
        configuration.GetSection("ApiSettings").Bind(Settings);

        Client = new FlurlClient(Settings.BaseUrl);
    }

    protected IFlurlRequest Api(string path) => Client.Request(path);

    public virtual async Task InitializeAsync()
    {
        // Feature 016: login por e-mail/senha, token no envelope Result (dados.token).
        var loginResponse = await Client.Request("auth/login")
            .PostJsonAsync(new { email = Settings.Email, password = Settings.Password });
        var loginJson = await loginResponse.GetJsonAsync<JsonElement>();
        var token = loginJson.GetProperty("dados").GetProperty("token").GetString();
        Client.WithHeader("Authorization", $"Bearer {token}");
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
