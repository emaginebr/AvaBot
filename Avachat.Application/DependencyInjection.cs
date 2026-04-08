using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Avachat.Infra.Context;
using Avachat.Infra.Interfaces.Repository;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.AppServices;
using Avachat.Infra.Repository;
using Avachat.Application.Services;

namespace Avachat.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAvachatServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AvachatContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AvachatContext")));

        // Repositories
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IKnowledgeFileRepository, KnowledgeFileRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

        // Domain Services
        services.AddScoped<AgentService>();
        services.AddScoped<IngestionService>();
        services.AddScoped<SearchService>();
        services.AddScoped<ChatService>();

        // App Services
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
        services.AddSingleton<IOpenAIService, OpenAIService>();

        return services;
    }
}
