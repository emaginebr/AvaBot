using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Avachat.Infra.Context;
using Avachat.Domain.Models;
using Avachat.Infra.Interfaces.Repository;
using Avachat.Infra.Interfaces.AppServices;
using Avachat.Infra.AppServices;
using Avachat.Infra.Repository;
using Avachat.Application.Profiles;
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
        services.AddScoped<IAgentRepository<Agent>, AgentRepository>();
        services.AddScoped<IKnowledgeFileRepository<KnowledgeFile>, KnowledgeFileRepository>();
        services.AddScoped<IChatSessionRepository<ChatSession>, ChatSessionRepository>();
        services.AddScoped<IChatMessageRepository<ChatMessage>, ChatMessageRepository>();

        // Domain Services
        services.AddScoped<AgentService>();
        services.AddScoped<IngestionService>();
        services.AddScoped<SearchService>();
        services.AddScoped<ChatService>();

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
        services.AddSingleton<IOpenAIService, OpenAIService>();

        return services;
    }
}
