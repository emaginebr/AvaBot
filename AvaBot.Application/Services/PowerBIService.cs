using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AvaBot.Application.Services;

public class PowerBIService
{
    private const string SecretMaskPrefix = "••••";
    private const int SecretHintLength = 4;
    private const int SchemaGenerationTimeoutSeconds = 60;
    private const int MaxQueryLogPageSize = 100;

    // Message do contrato (T030): com papel Viewer o executeQueries responde
    // PowerBIEntityNotFound, entao o erro de permissoes precisa de orientacao propria.
    private const string DatasetPermissionMessage =
        "Credenciais válidas, mas o aplicativo não tem permissão para consultar este dataset. " +
        "Conceda papel Contributor no workspace ou permissão Build no dataset e verifique a " +
        "configuração de tenant 'Dataset Execute Queries REST API'.";

    private readonly IAgentRepository<Agent> _agentRepository;
    private readonly IAgentPowerBIConfigRepository<AgentPowerBIConfig> _configRepository;
    private readonly IPowerBIDatasetRepository<PowerBIDataset> _datasetRepository;
    private readonly IPowerBIQueryLogRepository<PowerBIQueryLog> _queryLogRepository;
    private readonly IPowerBIClient _powerBIClient;
    private readonly ISecretProtector _secretProtector;
    private readonly PowerBISchemaBuilder _schemaBuilder;
    private readonly ILogger<PowerBIService> _logger;

    public PowerBIService(
        IAgentRepository<Agent> agentRepository,
        IAgentPowerBIConfigRepository<AgentPowerBIConfig> configRepository,
        IPowerBIDatasetRepository<PowerBIDataset> datasetRepository,
        IPowerBIQueryLogRepository<PowerBIQueryLog> queryLogRepository,
        IPowerBIClient powerBIClient,
        ISecretProtector secretProtector,
        PowerBISchemaBuilder schemaBuilder,
        ILogger<PowerBIService> logger)
    {
        _agentRepository = agentRepository;
        _configRepository = configRepository;
        _datasetRepository = datasetRepository;
        _queryLogRepository = queryLogRepository;
        _powerBIClient = powerBIClient;
        _secretProtector = secretProtector;
        _schemaBuilder = schemaBuilder;
        _logger = logger;
    }

    private async Task<Agent> GetAgentBySlugOrThrowAsync(string slug)
    {
        var agent = await _agentRepository.GetBySlugAsync(slug);
        if (agent == null)
            throw new KeyNotFoundException($"Agente '{slug}' nao encontrado");

        return agent;
    }

    private PowerBICredentials GetCredentials(AgentPowerBIConfig config)
    {
        return new PowerBICredentials
        {
            TenantId = config.TenantId,
            ClientId = config.ClientId,
            ClientSecret = _secretProtector.Unprotect(config.ClientSecretEncrypted)
        };
    }

    private async Task<AgentPowerBIConfig> RequireConfigAsync(Agent agent)
    {
        var config = await _configRepository.GetByAgentIdAsync(agent.AgentId);
        if (config == null || string.IsNullOrEmpty(config.ClientSecretEncrypted))
            throw new InvalidOperationException("Configure as credenciais do Power BI deste agente");

        return config;
    }

    // ---------- Credenciais e flag ----------

    public async Task<PowerBIConfigInfo> GetConfigAsync(string slug)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var config = await _configRepository.GetByAgentIdAsync(agent.AgentId);

        if (config == null)
        {
            return new PowerBIConfigInfo
            {
                AgentId = agent.AgentId,
                Enabled = agent.PowerBIEnabled,
                IsConfigured = false,
                HasClientSecret = false
            };
        }

        return MapConfigInfo(agent, config);
    }

    public async Task<PowerBIConfigInfo> SaveConfigAsync(string slug, PowerBIConfigUpdateInfo info)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var existing = await _configRepository.GetByAgentIdAsync(agent.AgentId);

        var plainSecret = info.ClientSecret?.Trim();

        if (existing == null && string.IsNullOrEmpty(plainSecret))
            throw new InvalidOperationException("Client Secret e obrigatorio na primeira configuracao");

        string secretToStore;
        string hint;

        if (!string.IsNullOrEmpty(plainSecret))
        {
            secretToStore = _secretProtector.Protect(plainSecret);
            hint = plainSecret.Length > SecretHintLength
                ? plainSecret[^SecretHintLength..]
                : plainSecret;
        }
        else
        {
            secretToStore = existing!.ClientSecretEncrypted;
            hint = existing.ClientSecretHint;
        }

        var credentialsChanged = existing == null
            || !string.Equals(existing.TenantId, info.TenantId.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(existing.ClientId, info.ClientId.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(secretToStore, existing.ClientSecretEncrypted, StringComparison.Ordinal);

        if (existing != null)
        {
            _powerBIClient.InvalidateToken(existing.TenantId, existing.ClientId);
            _powerBIClient.InvalidateToken(info.TenantId.Trim(), info.ClientId.Trim());
        }

        var config = existing ?? new AgentPowerBIConfig { AgentId = agent.AgentId };
        config.TenantId = info.TenantId.Trim();
        config.ClientId = info.ClientId.Trim();
        config.ClientSecretEncrypted = secretToStore;
        config.ClientSecretHint = hint;

        if (credentialsChanged)
        {
            config.LastTestAt = null;
            config.LastTestSuccess = null;
            config.LastTestMessage = null;
        }

        await _configRepository.UpsertAsync(config);

        _logger.LogInformation("Credenciais do Power BI salvas para o agente {AgentId}", agent.AgentId);

        return MapConfigInfo(agent, config);
    }

    public async Task<PowerBIConnectionTestInfo> TestConnectionAsync(string slug)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var config = await RequireConfigAsync(agent);
        var credentials = GetCredentials(config);

        var steps = new List<PowerBIConnectionTestStepInfo>();
        string? firstError = null;

        try
        {
            await _powerBIClient.GetAccessTokenAsync(credentials);
            steps.Add(new PowerBIConnectionTestStepInfo { Step = "auth", Success = true, Message = "Token obtido" });
        }
        catch (PowerBIApiException ex)
        {
            var message = $"Credenciais inválidas: {ex.Message}";
            steps.Add(new PowerBIConnectionTestStepInfo { Step = "auth", Success = false, Message = message });
            firstError = message;

            await PersistTestResultAsync(config, false, firstError);
            return new PowerBIConnectionTestInfo { Success = false, Steps = steps };
        }

        List<PowerBIWorkspace> workspaces;
        try
        {
            workspaces = await _powerBIClient.ListWorkspacesWithDatasetsAsync(credentials);
            steps.Add(new PowerBIConnectionTestStepInfo
            {
                Step = "workspaces",
                Success = true,
                Message = $"{workspaces.Count} workspace acessível" + (workspaces.Count == 1 ? "" : "is")
            });
        }
        catch (PowerBIApiException ex)
        {
            steps.Add(new PowerBIConnectionTestStepInfo { Step = "workspaces", Success = false, Message = ex.Message });
            await PersistTestResultAsync(config, false, ex.Message);
            return new PowerBIConnectionTestInfo { Success = false, Steps = steps };
        }

        var datasets = await _datasetRepository.GetByAgentIdAsync(agent.AgentId);

        foreach (var dataset in datasets)
        {
            var step = $"dataset:{dataset.Name}";

            try
            {
                await _powerBIClient.ExecuteQueryAsync(
                    credentials, dataset.WorkspaceId, dataset.DatasetId, "EVALUATE ROW(\"ok\", 1)");

                steps.Add(new PowerBIConnectionTestStepInfo { Step = step, Success = true, Message = "Consulta permitida" });
            }
            catch (PowerBIApiException ex) when (IsPermissionError(ex))
            {
                steps.Add(new PowerBIConnectionTestStepInfo { Step = step, Success = false, Message = DatasetPermissionMessage });
                firstError ??= DatasetPermissionMessage;
            }
            catch (PowerBIApiException ex)
            {
                steps.Add(new PowerBIConnectionTestStepInfo { Step = step, Success = false, Message = ex.Message });
                firstError ??= ex.Message;
            }
        }

        var success = firstError == null;
        await PersistTestResultAsync(config, success, firstError);

        return new PowerBIConnectionTestInfo { Success = success, Steps = steps };
    }

    public async Task<PowerBIConfigInfo> SetEnabledAsync(string slug, bool enabled)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);

        if (enabled)
        {
            var config = await _configRepository.GetByAgentIdAsync(agent.AgentId);
            if (config == null || string.IsNullOrEmpty(config.ClientSecretEncrypted))
                throw new InvalidOperationException("Configure as credenciais do Power BI deste agente");

            var datasets = await _datasetRepository.GetByAgentIdAsync(agent.AgentId);
            if (datasets.All(d => !d.IsUsable))
                throw new InvalidOperationException("Gere o schema de ao menos um dataset");
        }

        agent.PowerBIEnabled = enabled;
        await _agentRepository.UpdateAsync(agent);

        _logger.LogInformation("Power BI {State} para o agente {AgentId}",
            enabled ? "ativado" : "desativado", agent.AgentId);

        return await GetConfigAsync(slug);
    }

    // ---------- Descoberta ----------

    public async Task<List<PowerBIWorkspaceInfo>> ListWorkspacesAsync(string slug)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var config = await RequireConfigAsync(agent);
        var credentials = GetCredentials(config);

        var workspaces = await _powerBIClient.ListWorkspacesWithDatasetsAsync(credentials);

        return workspaces.Select(w => new PowerBIWorkspaceInfo
        {
            WorkspaceId = w.Id,
            Name = w.Name,
            Datasets = w.Datasets.Select(d => new PowerBIWorkspaceDatasetInfo
            {
                DatasetId = d.Id,
                Name = d.Name
            }).ToList()
        }).ToList();
    }

    // ---------- Datasets ----------

    public async Task<List<PowerBIDatasetInfo>> GetDatasetsAsync(string slug)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var datasets = await _datasetRepository.GetByAgentIdAsync(agent.AgentId);

        return datasets.Select(MapDatasetInfo).ToList();
    }

    public async Task<PowerBIDatasetInfo> CreateDatasetAsync(string slug, PowerBIDatasetInsertInfo info)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);

        if (await _datasetRepository.ExistsAsync(agent.AgentId, info.DatasetId.Trim()))
            throw new InvalidOperationException("Este dataset ja esta vinculado a este agente");

        var dataset = new PowerBIDataset
        {
            AgentId = agent.AgentId,
            WorkspaceId = info.WorkspaceId.Trim(),
            DatasetId = info.DatasetId.Trim(),
            Name = info.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(info.Description) ? null : info.Description.Trim(),
            ToolKey = await GenerateToolKeyAsync(agent.AgentId, info.Name),
            SchemaStatus = PowerBISchemaStatus.NotGenerated
        };

        await _datasetRepository.CreateAsync(dataset);

        return MapDatasetInfo(dataset);
    }

    public async Task<PowerBIDatasetInfo> UpdateDatasetAsync(string slug, long datasetId, PowerBIDatasetInsertInfo info)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var dataset = await _datasetRepository.GetByIdAsync(agent.AgentId, datasetId)
            ?? throw new KeyNotFoundException("Dataset nao encontrado");

        var newWorkspaceId = info.WorkspaceId.Trim();
        var newDatasetId = info.DatasetId.Trim();
        var newName = info.Name.Trim();

        var bindingChanged = !string.Equals(dataset.WorkspaceId, newWorkspaceId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(dataset.DatasetId, newDatasetId, StringComparison.OrdinalIgnoreCase);

        if (bindingChanged && await _datasetRepository.ExistsAsync(agent.AgentId, newDatasetId, dataset.PowerBIDatasetId))
            throw new InvalidOperationException("Este dataset ja esta vinculado a este agente");

        dataset.WorkspaceId = newWorkspaceId;
        dataset.DatasetId = newDatasetId;
        dataset.Description = string.IsNullOrWhiteSpace(info.Description) ? null : info.Description.Trim();

        if (!string.Equals(dataset.Name, newName, StringComparison.Ordinal))
        {
            dataset.Name = newName;
            dataset.ToolKey = await GenerateToolKeyAsync(agent.AgentId, newName, dataset.PowerBIDatasetId);
        }

        if (bindingChanged)
        {
            dataset.SchemaJson = null;
            dataset.SchemaStatus = PowerBISchemaStatus.NotGenerated;
            dataset.SchemaGeneratedAt = null;
            dataset.SchemaError = null;
        }

        await _datasetRepository.UpdateAsync(dataset);

        return MapDatasetInfo(dataset);
    }

    public async Task<string> DeleteDatasetAsync(string slug, long datasetId)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var dataset = await _datasetRepository.GetByIdAsync(agent.AgentId, datasetId)
            ?? throw new KeyNotFoundException("Dataset nao encontrado");

        await _datasetRepository.DeleteAsync(dataset.PowerBIDatasetId);

        var remaining = await _datasetRepository.GetByAgentIdAsync(agent.AgentId);

        if (agent.PowerBIEnabled && remaining.All(d => !d.IsUsable))
        {
            agent.PowerBIEnabled = false;
            await _agentRepository.UpdateAsync(agent);

            _logger.LogInformation("Power BI desativado para o agente {AgentId}: nenhum dataset utilizavel restante",
                agent.AgentId);

            return "Dataset removido. O Power BI foi desativado para este agente porque não há mais dataset com schema.";
        }

        return "Dataset removido";
    }

    // ---------- Schema ----------

    public async Task<PowerBIDatasetSchemaInfo> GenerateSchemaAsync(string slug, long datasetId)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var dataset = await _datasetRepository.GetByIdAsync(agent.AgentId, datasetId)
            ?? throw new KeyNotFoundException("Dataset nao encontrado");

        var config = await RequireConfigAsync(agent);
        var credentials = GetCredentials(config);

        using var timeoutSource = new CancellationTokenSource(
            TimeSpan.FromSeconds(SchemaGenerationTimeoutSeconds));

        try
        {
            var (schema, status) = await _schemaBuilder.BuildAsync(
                credentials, dataset.WorkspaceId, dataset.DatasetId, timeoutSource.Token);

            var previous = PowerBISchema.Deserialize(dataset.SchemaJson);
            var merged = previous != null ? PowerBISchema.MergeUserDescriptions(previous, schema) : schema;

            dataset.SchemaJson = PowerBISchema.Serialize(merged);
            dataset.SchemaStatus = status;
            dataset.SchemaGeneratedAt = DateTime.UtcNow;
            dataset.SchemaError = null;

            await _datasetRepository.UpdateAsync(dataset);

            _logger.LogInformation("Schema gerado para o dataset {DatasetId} do agente {AgentId} com {TableCount} tabelas e status {Status}",
                dataset.DatasetId, agent.AgentId, merged.Tables.Count, status);

            return MapSchemaInfo(dataset, merged);
        }
        catch (Exception ex) when (ex is PowerBIApiException or OperationCanceledException)
        {
            // FR-010: o schema anterior continua no ar; so o status e o erro mudam.
            var message = ex is OperationCanceledException
                ? $"A geração do schema excedeu {SchemaGenerationTimeoutSeconds} segundos"
                : IsPermissionError((PowerBIApiException)ex)
                    ? DatasetPermissionMessage
                    : ex.Message;

            dataset.SchemaStatus = PowerBISchemaStatus.Error;
            dataset.SchemaError = Truncate(message, 2000);
            await _datasetRepository.UpdateAsync(dataset);

            _logger.LogWarning("Falha ao gerar o schema do dataset {DatasetId} do agente {AgentId}: {Error}",
                dataset.DatasetId, agent.AgentId, message);

            throw new InvalidOperationException(message);
        }
    }

    public async Task<PowerBIDatasetSchemaInfo> GetSchemaAsync(string slug, long datasetId)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var dataset = await _datasetRepository.GetByIdAsync(agent.AgentId, datasetId)
            ?? throw new KeyNotFoundException("Dataset nao encontrado");

        return MapSchemaInfo(dataset, PowerBISchema.Deserialize(dataset.SchemaJson) ?? new PowerBISchema());
    }

    public async Task<PowerBIDatasetSchemaInfo> UpdateSchemaDescriptionsAsync(string slug, long datasetId, PowerBISchemaDescriptionUpdateInfo info)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);
        var dataset = await _datasetRepository.GetByIdAsync(agent.AgentId, datasetId)
            ?? throw new KeyNotFoundException("Dataset nao encontrado");

        var schema = PowerBISchema.Deserialize(dataset.SchemaJson)
            ?? throw new InvalidOperationException("Gere o schema antes de editar as descrições");

        var notFound = new List<string>();

        foreach (var item in info.Items)
        {
            var table = schema.Tables.FirstOrDefault(t => string.Equals(t.Name, item.Table, StringComparison.OrdinalIgnoreCase));

            if (table == null)
            {
                notFound.Add($"tabela '{item.Table}'");
                continue;
            }

            switch (item.Kind)
            {
                case "table":
                    table.UserDescription = NormalizeDescription(item.UserDescription);
                    break;

                case "column":
                    var column = table.Columns.FirstOrDefault(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                    if (column == null) notFound.Add($"coluna '{item.Table}[{item.Name}]'");
                    else column.UserDescription = NormalizeDescription(item.UserDescription);
                    break;

                case "measure":
                    var measure = table.Measures.FirstOrDefault(m => string.Equals(m.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                    if (measure == null) notFound.Add($"medida '{item.Table}[{item.Name}]'");
                    else measure.UserDescription = NormalizeDescription(item.UserDescription);
                    break;

                default:
                    notFound.Add($"item '{item.Kind}'");
                    break;
            }
        }

        if (notFound.Count > 0)
            throw new ArgumentException($"Item não encontrado no schema: {string.Join(", ", notFound)}");

        dataset.SchemaJson = PowerBISchema.Serialize(schema);
        await _datasetRepository.UpdateAsync(dataset);

        return MapSchemaInfo(dataset, schema);
    }

    // ---------- Histórico ----------

    public async Task<PowerBIQueryLogPageInfo> GetQueryLogsAsync(string slug, int page, int pageSize)
    {
        var agent = await GetAgentBySlugOrThrowAsync(slug);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxQueryLogPageSize) pageSize = MaxQueryLogPageSize;

        var (items, total) = await _queryLogRepository.GetPagedByAgentAsync(agent.AgentId, page, pageSize);

        return new PowerBIQueryLogPageInfo
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(l => new PowerBIQueryLogInfo
            {
                PowerBIQueryLogId = l.PowerBIQueryLogId,
                CreatedAt = l.CreatedAt,
                ChatSessionId = l.ChatSessionId,
                DatasetName = l.DatasetName,
                ToolName = l.ToolName,
                UserQuestion = l.UserQuestion,
                Query = l.Query,
                DurationMs = l.DurationMs,
                RowCount = l.RowCount,
                Truncated = l.Truncated,
                Status = (int)l.Status,
                ErrorMessage = l.ErrorMessage
            }).ToList()
        };
    }

    // ---------- Helpers ----------

    private async Task PersistTestResultAsync(AgentPowerBIConfig config, bool success, string? message)
    {
        config.LastTestAt = DateTime.UtcNow;
        config.LastTestSuccess = success;
        config.LastTestMessage = Truncate(message, 2000);
        await _configRepository.UpsertAsync(config);
    }

    private async Task<string> GenerateToolKeyAsync(long agentId, string name, long? excludeId = null)
    {
        var baseKey = AgentService.Slugify(name).Replace('-', '_');

        if (baseKey.Length > 60)
            baseKey = baseKey.TrimEnd('_')[..Math.Min(60, baseKey.TrimEnd('_').Length)];

        if (string.IsNullOrEmpty(baseKey))
            baseKey = "dataset";

        if (!await _datasetRepository.ToolKeyExistsAsync(agentId, baseKey, excludeId))
            return baseKey;

        for (var i = 2; ; i++)
        {
            var suffix = $"_{i}";
            var candidate = baseKey.Length + suffix.Length > 60
                ? baseKey[..(60 - suffix.Length)] + suffix
                : baseKey + suffix;

            if (!await _datasetRepository.ToolKeyExistsAsync(agentId, candidate, excludeId))
                return candidate;
        }
    }

    private static bool IsPermissionError(PowerBIApiException ex) =>
        ex.StatusCode is 401 or 403 or 404 || ex.ErrorCode == "PowerBIEntityNotFound";

    private static string? NormalizeDescription(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Truncate(value.Trim(), 1000);

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    private PowerBIConfigInfo MapConfigInfo(Agent agent, AgentPowerBIConfig config) => new()
    {
        AgentId = agent.AgentId,
        Enabled = agent.PowerBIEnabled,
        IsConfigured = true,
        TenantId = config.TenantId,
        ClientId = config.ClientId,
        HasClientSecret = !string.IsNullOrEmpty(config.ClientSecretEncrypted),
        ClientSecretMasked = string.IsNullOrEmpty(config.ClientSecretEncrypted)
            ? null
            : SecretMaskPrefix + config.ClientSecretHint,
        LastTestAt = config.LastTestAt,
        LastTestSuccess = config.LastTestSuccess,
        LastTestMessage = config.LastTestMessage
    };

    private static PowerBIDatasetInfo MapDatasetInfo(PowerBIDataset dataset)
    {
        var schema = PowerBISchema.Deserialize(dataset.SchemaJson);

        return new PowerBIDatasetInfo
        {
            PowerBIDatasetId = dataset.PowerBIDatasetId,
            WorkspaceId = dataset.WorkspaceId,
            DatasetId = dataset.DatasetId,
            Name = dataset.Name,
            Description = dataset.Description,
            ToolKey = dataset.ToolKey,
            SchemaStatus = (int)dataset.SchemaStatus,
            SchemaGeneratedAt = dataset.SchemaGeneratedAt,
            SchemaError = dataset.SchemaError,
            TableCount = schema?.Tables.Count ?? 0,
            ColumnCount = schema?.Tables.Sum(t => t.Columns.Count) ?? 0,
            MeasureCount = schema?.Tables.Sum(t => t.Measures.Count) ?? 0
        };
    }

    private static PowerBIDatasetSchemaInfo MapSchemaInfo(PowerBIDataset dataset, PowerBISchema schema) => new()
    {
        PowerBIDatasetId = dataset.PowerBIDatasetId,
        SchemaStatus = (int)dataset.SchemaStatus,
        SchemaGeneratedAt = dataset.SchemaGeneratedAt,
        SchemaError = dataset.SchemaError,
        Tables = schema.Tables.Select(t => new PowerBISchemaTableInfo
        {
            Name = t.Name,
            Description = t.Description,
            UserDescription = t.UserDescription,
            Columns = t.Columns.Select(c => new PowerBISchemaColumnInfo
            {
                Name = c.Name,
                DataType = c.DataType,
                Description = c.Description,
                UserDescription = c.UserDescription
            }).ToList(),
            Measures = t.Measures.Select(m => new PowerBISchemaMeasureInfo
            {
                Name = m.Name,
                Description = m.Description,
                UserDescription = m.UserDescription
            }).ToList()
        }).ToList()
    };
}
