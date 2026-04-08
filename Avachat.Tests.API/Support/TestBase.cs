using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;

namespace Avachat.Tests.API.Support;

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

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
