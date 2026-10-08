using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AvaBot.Application.Services;

public class PowerBIQueryLogCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int DefaultRetentionDays = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PowerBIQueryLogCleanupService> _logger;

    public PowerBIQueryLogCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PowerBIQueryLogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Falha na limpeza do historico de consultas do Power BI: {Error}", ex.Message);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync()
    {
        var retentionDays = int.TryParse(_configuration["PowerBI:QueryLogRetentionDays"], out var days)
            ? days
            : DefaultRetentionDays;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPowerBIQueryLogRepository<PowerBIQueryLog>>();

        var removed = await repository.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-retentionDays));

        if (removed > 0)
            _logger.LogInformation("Histórico de consultas do Power BI: {Removed} registro(s) com mais de {RetentionDays} dias removidos",
                removed, retentionDays);
    }
}
