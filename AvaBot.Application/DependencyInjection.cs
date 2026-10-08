using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AvaBot.Infra.Context;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.AppServices;
using AvaBot.Infra.Repository;
using AvaBot.Application.Profiles;
using AvaBot.Application.Services;

namespace AvaBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAvaBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AvaBotContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AvaBotContext")));

        // Repositories
        services.AddScoped<IAgentRepository<Agent>, AgentRepository>();
        services.AddScoped<IKnowledgeFileRepository<KnowledgeFile>, KnowledgeFileRepository>();
        services.AddScoped<IChatSessionRepository<ChatSession>, ChatSessionRepository>();
        services.AddScoped<IChatMessageRepository<ChatMessage>, ChatMessageRepository>();
        services.AddScoped<ITelegramChatRepository<TelegramChat>, TelegramChatRepository>();
        services.AddScoped<IAgentPowerBIConfigRepository<AgentPowerBIConfig>, AgentPowerBIConfigRepository>();
        services.AddScoped<IPowerBIDatasetRepository<PowerBIDataset>, PowerBIDatasetRepository>();
        services.AddScoped<IPowerBIQueryLogRepository<PowerBIQueryLog>, PowerBIQueryLogRepository>();

        // Domain Services
        services.AddScoped<AgentService>();
        services.AddScoped<IngestionService>();
        services.AddScoped<SearchService>();
        services.AddScoped<ChatService>();
        services.AddScoped<TelegramService>();
        services.AddScoped<WhatsappService>();
        services.AddScoped<PowerBIService>();
        services.AddScoped<PowerBISchemaBuilder>();
        services.AddScoped<PowerBIToolProvider>();

        // WPP Connect HttpClient
        services.AddHttpClient("WppConnect", (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(config["WppConnect:BaseUrl"] ?? "http://localhost:21465");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<IWppConnectService, WppConnectService>();

        // Power BI HttpClient
        services.AddHttpClient("PowerBI", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddHttpClient("EntraId", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Usado pelo diagnostico de chave (GET /models): sem Body/credencial padrao.
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddMemoryCache();
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddSingleton<IPowerBIClient, PowerBIClient>();

        services.AddHostedService<PowerBIQueryLogCleanupService>();

        // AutoMapper
        services.AddSingleton<AutoMapper.IMapper>(sp =>
        {
            var expression = new AutoMapper.MapperConfigurationExpression();
            expression.AddMaps(typeof(AgentProfile).Assembly);
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = new AutoMapper.MapperConfiguration(expression, loggerFactory);
            return config.CreateMapper();
        });

        // App Services
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        // Scoped: resolve a credencial do agente via repositorio (DbContext scoped),
        // entao nao pode ser singleton capturando uma dependencia de escopo menor.
        services.AddScoped<IOpenAIService, OpenAIService>();

        return services;
    }
}
