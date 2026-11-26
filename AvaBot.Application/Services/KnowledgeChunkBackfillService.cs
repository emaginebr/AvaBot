using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AvaBot.Application.Services;

/// <summary>
/// Migracao da base de conhecimento para o banco (feature 021). Na subida da API, reprocessa a partir
/// de file_content os arquivos prontos que ainda nao tem trechos, um por vez, cada um no seu scope.
/// Idempotente: so pega arquivo sem trecho, entao tambem cobre arquivos enviados pelo servidor antigo
/// durante a transicao. Falha de um arquivo vira status Error (visivel no painel) e nao para os demais.
/// Desliga-se com KnowledgeBackfill:Enabled=false.
/// </summary>
public class KnowledgeChunkBackfillService : BackgroundService
{
    public const string EnabledConfigKey = "KnowledgeBackfill:Enabled";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KnowledgeChunkBackfillService> _logger;

    public KnowledgeChunkBackfillService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<KnowledgeChunkBackfillService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue(EnabledConfigKey, true))
        {
            _logger.LogInformation("[Backfill] Desligado ({Key}=false)", EnabledConfigKey);
            return;
        }

        try
        {
            await RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[Backfill] Interrompido pelo desligamento da API; continua na proxima subida");
        }
        catch (Exception ex)
        {
            // Nao derruba a API: a busca segue funcionando para o que ja foi migrado.
            _logger.LogError(ex, "[Backfill] Falha ao migrar a base de conhecimento: {Error}", ex.Message);
        }
    }

    public async Task<BackfillResult> RunAsync(CancellationToken cancellationToken = default)
    {
        List<long> fileIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var chunkRepository = scope.ServiceProvider.GetRequiredService<IKnowledgeChunkRepository>();
            fileIds = await chunkRepository.GetFileIdsMissingChunksAsync();
        }

        var result = new BackfillResult { Total = fileIds.Count };
        if (fileIds.Count == 0)
        {
            _logger.LogInformation("[Backfill] Nenhum arquivo sem trechos; nada a migrar");
            return result;
        }

        _logger.LogInformation("[Backfill] {Count} arquivo(s) pronto(s) sem trechos; reprocessando", fileIds.Count);

        foreach (var fileId in fileIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var scope = _scopeFactory.CreateScope();
            var ingestion = scope.ServiceProvider.GetRequiredService<IngestionService>();
            var fileRepository = scope.ServiceProvider.GetRequiredService<IKnowledgeFileRepository<KnowledgeFile>>();

            // ProcessFileAsync nao lanca: em erro grava ProcessingStatus.Error no arquivo.
            await ingestion.ProcessFileAsync(fileId, allowMissingEmbeddings: true);

            var file = await fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                result.Skipped++;
            }
            else if (file.ProcessingStatus == ProcessingStatus.Ready)
            {
                result.Migrated++;
            }
            else
            {
                result.Failed++;
                _logger.LogWarning("[Backfill] Arquivo ID={FileId} ({FileName}) ficou com erro: {Error}",
                    fileId, file.FileName, file.ErrorMessage);
            }
        }

        _logger.LogInformation(
            "[Backfill] concluído: total={Total}, migrados={Migrated}, falhas={Failed}, ignorados={Skipped}",
            result.Total, result.Migrated, result.Failed, result.Skipped);

        return result;
    }
}

public class BackfillResult
{
    public int Total { get; set; }
    public int Migrated { get; set; }
    public int Failed { get; set; }
    /// <summary>Arquivos apagados enquanto a migracao rodava.</summary>
    public int Skipped { get; set; }
}
