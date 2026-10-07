using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AvaBot.Application.Services;

public class PowerBIToolProvider
{
    private readonly IAgentPowerBIConfigRepository<AgentPowerBIConfig> _configRepository;
    private readonly IPowerBIDatasetRepository<PowerBIDataset> _datasetRepository;
    private readonly IPowerBIQueryLogRepository<PowerBIQueryLog> _queryLogRepository;
    private readonly IPowerBIClient _powerBIClient;
    private readonly ISecretProtector _secretProtector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PowerBIToolProvider> _logger;

    public PowerBIToolProvider(
        IAgentPowerBIConfigRepository<AgentPowerBIConfig> configRepository,
        IPowerBIDatasetRepository<PowerBIDataset> datasetRepository,
        IPowerBIQueryLogRepository<PowerBIQueryLog> queryLogRepository,
        IPowerBIClient powerBIClient,
        ISecretProtector secretProtector,
        IConfiguration configuration,
        ILogger<PowerBIToolProvider> logger)
    {
        _configRepository = configRepository;
        _datasetRepository = datasetRepository;
        _queryLogRepository = queryLogRepository;
        _powerBIClient = powerBIClient;
        _secretProtector = secretProtector;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PowerBIToolset?> GetToolsetAsync(Agent agent, long? sessionId, string userQuestion)
    {
        if (!agent.PowerBIEnabled)
            return null;

        var config = await _configRepository.GetByAgentIdAsync(agent.AgentId);
        if (config == null || string.IsNullOrEmpty(config.ClientSecretEncrypted))
            return null;

        var datasets = (await _datasetRepository.GetByAgentIdAsync(agent.AgentId))
            .Where(d => d.IsUsable)
            .ToList();

        if (datasets.Count == 0)
            return null;

        PowerBICredentials credentials;

        try
        {
            credentials = new PowerBICredentials
            {
                TenantId = config.TenantId,
                ClientId = config.ClientId,
                ClientSecret = _secretProtector.Unprotect(config.ClientSecretEncrypted)
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Credenciais do Power BI do agente {AgentId} nao podem ser decodificadas: {Error}",
                agent.AgentId, ex.Message);
            return null;
        }

        return new PowerBIToolset(
            agent,
            credentials,
            datasets,
            sessionId,
            userQuestion,
            _powerBIClient,
            _queryLogRepository,
            GetInt("PowerBI:MaxRows", 100),
            GetInt("PowerBI:QueryTimeoutSeconds", 30),
            _logger);
    }

    private int GetInt(string key, int fallback) =>
        int.TryParse(_configuration[key], out var value) ? value : fallback;
}

public class PowerBIToolset
{
    public const string ListSchemaToolName = "listar_schema";
    public const string QueryToolName = "consultar_bi";

    private const int ResultPreviewLength = 2000;

    private const string DatasetPermissionMessage =
        "Credenciais válidas, mas o aplicativo não tem permissão para consultar este dataset. " +
        "Conceda papel Contributor no workspace ou permissão Build no dataset e verifique a " +
        "configuração de tenant 'Dataset Execute Queries REST API'.";

    // Bloco de contracts/llm-tools.md, acrescentado ao system prompt quando ha tools (FR-022).
    public const string PromptAddendum =
        "DADOS DO POWER BI: Você tem acesso a ferramentas que consultam dados reais no Power BI.\n" +
        "- Use as ferramentas quando a pergunta exigir números/dados; para outras perguntas use a base de conhecimento.\n" +
        "- Se faltar informação necessária para a consulta (ex.: período, produto, indicador), PERGUNTE ao usuário antes de consultar. Não assuma valores padrão.\n" +
        "- Antes da primeira consulta a um dataset, chame listar_schema.\n" +
        "- Use SOMENTE valores retornados pelas ferramentas. NUNCA invente ou estime números.\n" +
        "- Se a consulta falhar, informe que não foi possível obter os dados no momento.\n" +
        "- Responda em texto; pode usar listas destacando os principais valores. NÃO use tabelas. Se houver muitas linhas, resuma (totais, maiores e menores valores).";

    private readonly Agent _agent;
    private readonly PowerBICredentials _credentials;
    private readonly List<PowerBIDataset> _datasets;
    private readonly long? _sessionId;
    private readonly string _userQuestion;
    private readonly IPowerBIClient _powerBIClient;
    private readonly IPowerBIQueryLogRepository<PowerBIQueryLog> _queryLogRepository;
    private readonly int _maxRows;
    private readonly int _queryTimeoutSeconds;
    private readonly ILogger _logger;

    internal PowerBIToolset(
        Agent agent,
        PowerBICredentials credentials,
        List<PowerBIDataset> datasets,
        long? sessionId,
        string userQuestion,
        IPowerBIClient powerBIClient,
        IPowerBIQueryLogRepository<PowerBIQueryLog> queryLogRepository,
        int maxRows,
        int queryTimeoutSeconds,
        ILogger logger)
    {
        _agent = agent;
        _credentials = credentials;
        _datasets = datasets;
        _sessionId = sessionId;
        _userQuestion = userQuestion;
        _powerBIClient = powerBIClient;
        _queryLogRepository = queryLogRepository;
        _maxRows = maxRows;
        _queryTimeoutSeconds = queryTimeoutSeconds;
        _logger = logger;

        var datasetEnum = BuildDatasetEnum();

        Definitions = new List<ChatToolDefinition>
        {
            new()
            {
                Name = ListSchemaToolName,
                Description = "Retorna tabelas, colunas (com tipo) e medidas de um dataset do Power BI. " +
                    $"Chame antes da primeira consulta a um dataset. Datasets disponíveis:\n{BuildCatalog()}",
                ParametersJsonSchema = "{\"type\":\"object\",\"properties\":{\"dataset\":{\"type\":\"string\",\"enum\":"
                    + datasetEnum + "}},\"required\":[\"dataset\"],\"additionalProperties\":false}"
            },
            new()
            {
                Name = QueryToolName,
                Description = "Executa uma consulta DAX somente leitura (deve começar com EVALUATE ou DEFINE) " +
                    $"em um dataset do Power BI e retorna as linhas. Prefira agregações (SUMMARIZECOLUMNS) e TOPN; " +
                    $"o resultado é limitado a {maxRows} linhas.",
                ParametersJsonSchema = "{\"type\":\"object\",\"properties\":{\"dataset\":{\"type\":\"string\",\"enum\":"
                    + datasetEnum + "},\"dax\":{\"type\":\"string\",\"description\":\"Consulta DAX completa\"}}" +
                    ",\"required\":[\"dataset\",\"dax\"],\"additionalProperties\":false}"
            }
        };
    }

    public IReadOnlyList<ChatToolDefinition> Definitions { get; }

    public string SystemPromptAddendum => PromptAddendum;

    public List<AgentTestPowerBIQueryInfo> ExecutedQueries { get; } = new();

    public async Task<string> ExecuteAsync(ChatToolCall toolCall, CancellationToken cancellationToken)
    {
        try
        {
            return toolCall.Name switch
            {
                ListSchemaToolName => await ExecuteListSchemaAsync(toolCall.ArgumentsJson, cancellationToken),
                QueryToolName => await ExecuteQueryAsync(toolCall.ArgumentsJson, cancellationToken),
                _ => await FailAsync(null, toolCall.Name, null, $"Ferramenta desconhecida: {toolCall.Name}", cancellationToken)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Nunca lancar para o loop de tools: a conversa continua (FR-026).
            return ErrorJson(Sanitize(ex.Message));
        }
    }

    private async Task<string> ExecuteListSchemaAsync(string argumentsJson, CancellationToken cancellationToken)
    {
        var dataset = ResolveDataset(argumentsJson, out var resolveError);

        if (dataset == null)
            return await FailAsync(null, ListSchemaToolName, null, resolveError!, cancellationToken);

        var schema = PowerBISchema.Deserialize(dataset.SchemaJson);

        if (schema == null)
        {
            const string message = "Schema deste dataset está corrompido. Gere o schema novamente pelo painel.";
            return await FailAsync(dataset, ListSchemaToolName, null, message, cancellationToken);
        }

        var text = RenderSchemaText(dataset, schema);

        // Regra 5 de contracts/llm-tools.md: listar_schema tambem e registrado, com duracao ~0.
        await WriteLogAsync(dataset, ListSchemaToolName, null, 0, PowerBIQueryStatus.Success,
            null, schema.Tables.Count, false);

        ExecutedQueries.Add(new AgentTestPowerBIQueryInfo
        {
            ToolName = ListSchemaToolName,
            DatasetName = dataset.Name,
            DurationMs = 0,
            RowCount = schema.Tables.Count,
            Success = true,
            ResultPreview = Truncate(text, ResultPreviewLength)
        });

        return text;
    }

    private async Task<string> ExecuteQueryAsync(string argumentsJson, CancellationToken cancellationToken)
    {
        var dataset = ResolveDataset(argumentsJson, out var resolveError);

        if (dataset == null)
            return await FailAsync(null, QueryToolName, null, resolveError!, cancellationToken);

        var dax = ReadDaxArgument(argumentsJson);

        if (string.IsNullOrWhiteSpace(dax) || !StartsWithQueryKeyword(dax))
        {
            return await FailAsync(dataset, QueryToolName, dax,
                "A consulta DAX deve começar com EVALUATE ou DEFINE.", cancellationToken);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(_queryTimeoutSeconds));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var queryResult = await _powerBIClient.ExecuteQueryAsync(
                _credentials, dataset.WorkspaceId, dataset.DatasetId, dax, timeoutSource.Token);

            stopwatch.Stop();

            var rows = queryResult.Rows;
            var truncated = rows.Count > _maxRows;

            if (truncated)
                rows = rows.GetRange(0, _maxRows);

            var json = JsonSerializer.Serialize(new
            {
                columns = queryResult.Columns,
                rows,
                rowCount = rows.Count,
                truncated
            });

            await WriteLogAsync(dataset, QueryToolName, dax, (int)stopwatch.ElapsedMilliseconds,
                PowerBIQueryStatus.Success, null, rows.Count, truncated);

            ExecutedQueries.Add(new AgentTestPowerBIQueryInfo
            {
                ToolName = QueryToolName,
                DatasetName = dataset.Name,
                Query = dax,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                RowCount = rows.Count,
                Truncated = truncated,
                Success = true,
                ResultPreview = Truncate(json, ResultPreviewLength)
            });

            return json;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return await FailAsync(dataset, QueryToolName, dax,
                $"A consulta excedeu {_queryTimeoutSeconds} segundos.",
                CancellationToken.None, PowerBIQueryStatus.Timeout, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (PowerBIApiException ex)
        {
            stopwatch.Stop();
            var message = IsPermissionError(ex) ? DatasetPermissionMessage : ex.Message;

            return await FailAsync(dataset, QueryToolName, dax, message,
                CancellationToken.None, PowerBIQueryStatus.Error, (int)stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<string> FailAsync(
        PowerBIDataset? dataset,
        string toolName,
        string? query,
        string message,
        CancellationToken cancellationToken,
        PowerBIQueryStatus status = PowerBIQueryStatus.Error,
        int durationMs = 0)
    {
        await WriteLogAsync(dataset, toolName, query, durationMs, status, message, null, false);

        ExecutedQueries.Add(new AgentTestPowerBIQueryInfo
        {
            ToolName = toolName,
            DatasetName = dataset?.Name,
            Query = query,
            DurationMs = durationMs,
            Success = false,
            Error = Truncate(message, 2000)
        });

        return ErrorJson(Sanitize(message));
    }

    private static string? ReadDaxArgument(string argumentsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            if (doc.RootElement.TryGetProperty("dax", out var dax) && dax.ValueKind == JsonValueKind.String)
                return dax.GetString();
        }
        catch (JsonException)
        {
            // argumento invalido: trata como ausencia de dax
        }

        return null;
    }

    private PowerBIDataset? ResolveDataset(string argumentsJson, out string? error)
    {
        error = null;

        string? key = null;

        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            if (doc.RootElement.TryGetProperty("dataset", out var datasetElement)
                && datasetElement.ValueKind == JsonValueKind.String)
                key = datasetElement.GetString();
        }
        catch (JsonException)
        {
            error = "Argumentos da ferramenta não são um JSON válido.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            error = "Informe o dataset.";
            return null;
        }

        var dataset = _datasets.FirstOrDefault(d => string.Equals(d.ToolKey, key, StringComparison.Ordinal));

        if (dataset == null)
            error = "Dataset não disponível para este agente.";

        return dataset;
    }

    private static bool IsPermissionError(PowerBIApiException ex) =>
        ex.StatusCode is 401 or 403 or 404 || ex.ErrorCode == "PowerBIEntityNotFound";

    // Defesa extra alem da API ser somente leitura (regra 2 de contracts/llm-tools.md).
    private static bool StartsWithQueryKeyword(string dax)
    {
        var text = dax.TrimStart();

        while (true)
        {
            if (text.StartsWith("//") || text.StartsWith("--"))
            {
                var lineBreak = text.IndexOf('\n');
                if (lineBreak < 0) return false;
                text = text[(lineBreak + 1)..].TrimStart();
            }
            else if (text.StartsWith("/*"))
            {
                var end = text.IndexOf("*/", StringComparison.Ordinal);
                if (end < 0) return false;
                text = text[(end + 2)..].TrimStart();
            }
            else
            {
                break;
            }
        }

        return text.StartsWith("EVALUATE", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("DEFINE", StringComparison.OrdinalIgnoreCase);
    }

    private static string RenderSchemaText(PowerBIDataset dataset, PowerBISchema schema)
    {
        var sb = new StringBuilder();
        sb.Append("Dataset: ").AppendLine(dataset.Name);

        foreach (var table in schema.Tables)
        {
            sb.Append("Tabela ").Append(table.Name);

            var tableDescription = table.UserDescription ?? table.Description;
            if (!string.IsNullOrWhiteSpace(tableDescription))
                sb.Append(" — ").Append(tableDescription);

            sb.AppendLine();

            if (table.Columns.Count > 0)
                sb.Append("  Colunas: ")
                  .AppendLine(string.Join("; ", table.Columns.Select(DescribeColumn)));

            if (table.Measures.Count > 0)
                sb.Append("  Medidas: ")
                  .AppendLine(string.Join("; ", table.Measures.Select(DescribeMeasure)));
        }

        return sb.ToString().TrimEnd();
    }

    private static string DescribeColumn(PowerBISchemaColumn column)
    {
        var text = column.Name;

        if (!string.IsNullOrWhiteSpace(column.DataType))
            text += $" ({column.DataType})";

        var description = column.UserDescription ?? column.Description;
        if (!string.IsNullOrWhiteSpace(description))
            text += $" — {description}";

        return text;
    }

    private static string DescribeMeasure(PowerBISchemaMeasure measure)
    {
        var text = $"[{measure.Name}]";

        var description = measure.UserDescription ?? measure.Description;
        if (!string.IsNullOrWhiteSpace(description))
            text += $" — {description}";

        return text;
    }

    private string BuildCatalog() =>
        string.Join("\n", _datasets.Select(d =>
            $"- {d.ToolKey}: {d.Name}" + (string.IsNullOrWhiteSpace(d.Description) ? "" : $" — {d.Description}")));

    private string BuildDatasetEnum() =>
        JsonSerializer.Serialize(_datasets.Select(d => d.ToolKey).ToList());

    private static string ErrorJson(string message) =>
        JsonSerializer.Serialize(new Dictionary<string, string> { ["error"] = message });

    private string Sanitize(string message)
    {
        // FR-027: nem o segredo nem outra credencial podem chegar ao modelo ou ao historico.
        var sanitized = string.IsNullOrEmpty(_credentials.ClientSecret)
            ? message
            : message.Replace(_credentials.ClientSecret, "[redacted]");

        return sanitized.Length > 2000 ? sanitized[..2000] : sanitized;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    private async Task WriteLogAsync(
        PowerBIDataset? dataset,
        string toolName,
        string? query,
        int durationMs,
        PowerBIQueryStatus status,
        string? errorMessage,
        int? rowCount,
        bool truncated)
    {
        _logger.LogInformation(
            "\n┌─── POWER BI TOOL ──────────────────────────────────────┐\n" +
            "[{ToolName}] dataset={DatasetName} status={Status} linhas={RowCount} {DurationMs}ms\n" +
            "{Query}\n" +
            "└─────────────────────────────────────────────────────────┘\n",
            toolName,
            dataset?.Name ?? "(nenhum)",
            status,
            rowCount?.ToString() ?? "-",
            durationMs,
            Truncate(query, 300) ?? "(nenhuma)");

        try
        {
            await _queryLogRepository.CreateAsync(new PowerBIQueryLog
            {
                AgentId = _agent.AgentId,
                ChatSessionId = _sessionId,
                PowerBIDatasetId = dataset?.PowerBIDatasetId,
                DatasetName = dataset?.Name,
                ToolName = toolName,
                UserQuestion = _userQuestion,
                Query = query,
                DurationMs = durationMs,
                RowCount = rowCount,
                Truncated = truncated,
                Status = status,
                ErrorMessage = errorMessage == null ? null : Truncate(Sanitize(errorMessage), 2000)
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Falha ao registrar o historico de consulta do Power BI: {Error}", ex.Message);
        }
    }
}
