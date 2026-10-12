using AvaBot.Infra.Context;
using Microsoft.EntityFrameworkCore;

namespace AvaBot.API.Database;

/// <summary>
/// Aplica as migracoes EF pendentes na subida da API, antes de qualquer outro acesso ao banco.
/// Bancos criados antes desta versao (schema via avabot.sql + scripts/*.sql) nao tem a tabela
/// __EFMigrationsHistory; neles e preciso rodar scripts/seed-ef-migrations-history.sql uma vez,
/// senao o EF tentaria reaplicar todas as migracoes desde o inicio.
/// </summary>
public static class StartupMigrator
{
    public const string ConfigKey = "Database:MigrateOnStartup";

    public const string MissingHistoryMessage =
        "Nenhuma migracao registrada em __EFMigrationsHistory (tabela ausente ou vazia), entao a API nao consegue " +
        "saber quais migracoes ja foram aplicadas neste banco. Rode scripts/seed-ef-migrations-history.sql uma vez " +
        "(em um banco novo, antes dele, avabot.sql) e suba a API de novo. Para subir sem aplicar migracoes, " +
        "configure Database:MigrateOnStartup=false.";

    public static async Task MigrateAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(StartupMigrator));

        if (!configuration.GetValue(ConfigKey, true))
        {
            logger.LogWarning("Migracoes automaticas desligadas ({ConfigKey}=false); o schema do banco nao foi verificado", ConfigKey);
            return;
        }

        var context = scope.ServiceProvider.GetRequiredService<AvaBotContext>();

        // Nao existe migracao inicial (o schema base vem de avabot.sql); um historico vazio nunca e um
        // banco novo a criar do zero, e sim um banco legado que precisa do baseline. Sem esta guarda o EF
        // tentaria aplicar tudo desde a primeira migracao e falharia em "coluna ja existe".
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (!applied.Any())
        {
            throw new InvalidOperationException(MissingHistoryMessage);
        }

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Banco de dados atualizado; nenhuma migracao pendente");
            return;
        }

        logger.LogInformation("Aplicando {Count} migracao(oes) pendente(s): {Migrations}", pending.Count, string.Join(", ", pending));
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migracoes aplicadas com sucesso");
    }
}
