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
            GetPositiveInt("PowerBI:MaxQueryAttempts", PowerBIToolset.DefaultMaxQueryAttempts),
            _logger);
    }

    private int GetInt(string key, int fallback) =>
        int.TryParse(_configuration[key], out var value) ? value : fallback;

    // Ausente, nao numerico, zero ou negativo: cai no padrao (data-model.md, validade da chave).
    private int GetPositiveInt(string key, int fallback)
    {
        var value = GetInt(key, fallback);
        return value > 0 ? value : fallback;
    }
}

public class PowerBIToolset
{
    public const string ListSchemaToolName = "listar_schema";
    public const string QueryToolName = "consultar_bi";

    // Categorias de contracts/powerbi-query-error.md.
    public const string CategoryDaxQuery = "dax_query";
    public const string CategoryTransientService = "transient_service";
    public const string CategoryAuthOrPermission = "authentication_or_permission";
    public const string CategoryAttemptLimit = "attempt_limit";

    public const int DefaultMaxQueryAttempts = 5;
    private static readonly TimeSpan BackoffBase = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan BackoffCeiling = TimeSpan.FromSeconds(30);

    private const string DatasetPermissionMessage =
        "Credenciais válidas, mas o aplicativo não tem permissão para consultar este dataset. " +
        "Conceda papel Contributor no workspace ou permissão Build no dataset e verifique a " +
        "configuração de tenant 'Dataset Execute Queries REST API'.";

    // Bloco de contracts/llm-tools.md, acrescentado ao system prompt quando ha tools (FR-022).
    public const string PromptAddendum =
        "DADOS DO POWER BI: Você tem acesso a ferramentas que consultam dados reais no Power BI.\n" +
        "- Use as ferramentas quando a pergunta exigir números/dados; para outras perguntas use a base de conhecimento.\n" +
        "- Para perguntas que exigem dados do BI, consulte o dataset apropriado; não responda usando apenas conhecimento geral ou a base de conhecimento.\n" +
        "- Se a pergunta do usuário não informar algo necessário (ex.: período, produto, indicador, unidade ou escopo geográfico), PERGUNTE antes de consultar. Não invente nem assuma valores padrão, inclusive o ano mais recente.\n" +
        "- Antes da primeira consulta a um dataset, chame listar_schema.\n" +
        "- Escreva consultas exclusivamente em DAX válido para Power BI. Não use sintaxe SQL, como LIMIT, OFFSET, FETCH, SELECT ou FROM.\n" +
        "- A consulta deve começar com EVALUATE ou DEFINE. Para limitar linhas, use TOPN dentro da expressão DAX; nunca acrescente LIMIT ao final.\n" +
        "- Use apenas tabelas, colunas e medidas existentes no schema retornado por listar_schema, copiando os nomes exatamente como aparecem (ex.: 'Nome da Tabela'[Coluna], [Medida]); não encurte nem remova prefixos.\n" +
        "- Estrutura DAX: 'DEFINE' (opcional) aceita apenas VAR/MEASURE/TABLE/COLUMN e é seguido de EVALUATE; não existe RETURN no nível do DEFINE. Exemplo: DEFINE VAR _ano = \"2025\" EVALUATE TOPN(10, SUMMARIZECOLUMNS('T'[Col], \"Total\", SUM('T'[Valor])), [Total], DESC).\n" +
        "- Em TOPN, ordene por uma coluna ou medida e use DESC ou ASC; nunca ordene por constante (empates devolvem todas as linhas). Respeite o tipo da coluna no schema (Text compara com texto entre aspas).\n" +
        "- Medidas são expressões, nunca colunas de agrupamento. Para um valor único: EVALUATE ROW(\"Valor\", CALCULATE([Medida], 'Dim'[Col] = \"x\", 'Calendar'[Year] = 2024)). Por categoria: SUMMARIZECOLUMNS('Dim'[Col], \"Valor\", [Medida]).\n" +
        "- Leia a fórmula de cada medida no schema para saber que filtros ela já aplica, e use os relacionamentos do schema para filtrar a tabela de fatos pelas dimensões.\n" +
        "- Quando a coluna lista seus valores no schema, filtre pelo valor exato listado. Para achar um produto, país ou categoria pelo nome, não liste a tabela inteira: filtre com CONTAINSSTRING, ex.: FILTER(VALUES('Dim'[Col]), CONTAINSSTRING('Dim'[Col], \"termo\")), tentando também sem acento ou em inglês.\n" +
        "- Se a dúvida for sobre como o dado está modelado (qual coluna, código ou valor representa algo), investigue com as ferramentas usando as tentativas restantes; só pergunte ao usuário o que depende da intenção dele.\n" +
        "- Use SOMENTE valores retornados pelas ferramentas. NUNCA invente ou estime números.\n" +
        "- Se consultar_bi devolver erro com queryMayBeCorrected true, use message, errorCode e responseBody do diagnóstico para corrigir a DAX e tentar de novo, usando apenas tabelas, colunas e medidas do schema. Não troque a pergunta nem invente correção fora do diagnóstico.\n" +
        "- Se queryMayBeCorrected for false (autenticação, permissão ou limite de tentativas esgotado), NÃO reenvie a consulta: informe que não foi possível obter os dados no momento.\n" +
        "- Falhas temporárias já foram repetidas automaticamente; você não precisa insistir nelas.\n" +
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
    private readonly Random _jitter = new();

    // Orcamento por mensagem: execucoes reais de consultar_bi (inicial + replays + DAX corrigida).
    private int _queryAttempts;

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
        int maxQueryAttempts,
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
        MaxQueryAttempts = maxQueryAttempts > 0 ? maxQueryAttempts : DefaultMaxQueryAttempts;
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
                Description = "Executa uma consulta somente leitura escrita exclusivamente em DAX válido (deve começar com EVALUATE ou DEFINE; não use sintaxe SQL como LIMIT). " +
                    $"Use TOPN dentro da expressão DAX para limitar linhas. Prefira agregações (SUMMARIZECOLUMNS) e TOPN; " +
                    $"o resultado é limitado a {maxRows} linhas.",
                ParametersJsonSchema = "{\"type\":\"object\",\"properties\":{\"dataset\":{\"type\":\"string\",\"enum\":"
                    + datasetEnum + "},\"dax\":{\"type\":\"string\",\"description\":\"Consulta DAX completa\"}}" +
                    ",\"required\":[\"dataset\",\"dax\"],\"additionalProperties\":false}"
            }
        };
    }

    public IReadOnlyList<ChatToolDefinition> Definitions { get; }

    /// <summary>Limite de execucoes de consultar_bi por mensagem; ChatService usa isso para dimensionar o loop de tools.</summary>
    public int MaxQueryAttempts { get; }

    /// <summary>Chamadas de tool que o loop OpenAI precisa permitir: o orcamento BI mais a leitura auxiliar de schema.</summary>
    public int ModelToolCallBudget => MaxQueryAttempts + 1;

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
            ResultPreview = text
        });

        return text;
    }

    private async Task<string> ExecuteQueryAsync(string argumentsJson, CancellationToken cancellationToken)
    {
        var dataset = ResolveDataset(argumentsJson, out var resolveError);

        if (dataset == null)
            return await FailLocalQueryAsync(null, null, resolveError!);

        var dax = ReadDaxArgument(argumentsJson);

        if (string.IsNullOrWhiteSpace(dax) || !StartsWithQueryKeyword(dax))
        {
            return await FailLocalQueryAsync(dataset, dax,
                "A consulta DAX deve começar com EVALUATE ou DEFINE. Não use sintaxe SQL (SELECT, FROM, LIMIT); " +
                "para limitar linhas use TOPN dentro da expressão DAX.");
        }

        QueryFailure? lastFailure = null;

        // D3: o orcamento conta execucoes reais de consultar_bi (inicial, replays e DAX corrigida).
        while (_queryAttempts < MaxQueryAttempts)
        {
            _queryAttempts++;

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(_queryTimeoutSeconds));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var queryResult = await _powerBIClient.ExecuteQueryAsync(
                    _credentials, dataset.WorkspaceId, dataset.DatasetId, dax, timeoutSource.Token);

                stopwatch.Stop();
                return await CompleteQueryAsync(dataset, dax, queryResult, (int)stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento do usuario nao e falha transitaria: nao gera nova tentativa (plan:2).
                throw;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                lastFailure = QueryFailure.OfTimeout((int)stopwatch.ElapsedMilliseconds,
                    $"A consulta excedeu {_queryTimeoutSeconds} segundos.");
            }
            catch (PowerBIApiException ex)
            {
                stopwatch.Stop();
                lastFailure = Classify(ex, (int)stopwatch.ElapsedMilliseconds);
            }

            // Cada requisicao que falha tem registro proprio, inclusive os replays transitarios.
            await RecordFailureAsync(dataset, dax, lastFailure);

            if (!lastFailure.Transient)
                return QueryErrorJson(lastFailure, CanModelCorrect(lastFailure));

            if (_queryAttempts >= MaxQueryAttempts)
                break;

            await Task.Delay(NextDelay(lastFailure.RetryAfter), cancellationToken);
        }

        // Sem orcamento: nenhuma outra requisicao e enviada ao Power BI, e o modelo nao e convidado a corrigir.
        var limitFailure = lastFailure ?? QueryFailure.OfLimitReached(
            $"O limite de {MaxQueryAttempts} execucoes da consulta foi atingido; nenhuma nova requisicao foi enviada.");

        return QueryErrorJson(limitFailure with { Category = CategoryAttemptLimit }, queryMayBeCorrected: false);
    }

    private async Task<string> CompleteQueryAsync(
        PowerBIDataset dataset, string dax, PowerBIQueryResult queryResult, int durationMs)
    {
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

        await WriteLogAsync(dataset, QueryToolName, dax, durationMs,
            PowerBIQueryStatus.Success, null, rows.Count, truncated);

        ExecutedQueries.Add(new AgentTestPowerBIQueryInfo
        {
            ToolName = QueryToolName,
            DatasetName = dataset.Name,
            Query = dax,
            DurationMs = durationMs,
            RowCount = rows.Count,
            Truncated = truncated,
            Success = true,
            ResultPreview = json
        });

        return json;
    }

    private static QueryFailure Classify(PowerBIApiException ex, int durationMs)
    {
        var transient = ex.StatusCode is 429 or 0 || ex.StatusCode >= 500;

        if (IsPermissionError(ex))
        {
            return QueryFailure.Of(CategoryAuthOrPermission, PowerBIQueryStatus.Error, durationMs,
                DatasetPermissionMessage, ex.StatusCode, ex.ErrorCode, ex.ResponseBody, null, transient: false);
        }

        if (transient)
        {
            return QueryFailure.Of(CategoryTransientService,
                ex.StatusCode == 0 ? PowerBIQueryStatus.Timeout : PowerBIQueryStatus.Error,
                durationMs, ex.Message, ex.StatusCode == 0 ? null : ex.StatusCode, ex.ErrorCode,
                ex.ResponseBody, ex.RetryAfter, transient: true);
        }

        return QueryFailure.Of(CategoryDaxQuery, PowerBIQueryStatus.Error, durationMs,
            ex.Message, ex.StatusCode, ex.ErrorCode, ex.ResponseBody, null, transient: false);
    }

    private static bool CanModelCorrect(QueryFailure failure) =>
        failure.Category == CategoryDaxQuery;

    private async Task<string> FailLocalQueryAsync(
        PowerBIDataset? dataset, string? dax, string message)
    {
        var failure = QueryFailure.Of(CategoryDaxQuery, PowerBIQueryStatus.Error, 0,
            message, null, null, null, null, transient: false);

        // Rejeicao local nao enviou requisicao: nao consome orcamento, mas fica no historico.
        await RecordFailureAsync(dataset, dax, failure);

        return QueryErrorJson(failure, queryMayBeCorrected: true);
    }

    private async Task RecordFailureAsync(PowerBIDataset? dataset, string? dax, QueryFailure failure)
    {
        var diagnostic = BuildDiagnostic(failure, queryMayBeCorrected: CanModelCorrect(failure));

        await WriteLogAsync(dataset, QueryToolName, dax, failure.DurationMs,
            failure.Status, JsonSerializer.Serialize(diagnostic), null, false);

        ExecutedQueries.Add(new AgentTestPowerBIQueryInfo
        {
            ToolName = QueryToolName,
            DatasetName = dataset?.Name,
            Query = dax,
            DurationMs = failure.DurationMs,
            Success = false,
            Error = failure.Diagnosis
        });
    }

    private object BuildDiagnostic(QueryFailure failure, bool queryMayBeCorrected) => new
    {
        category = failure.Category,
        statusCode = failure.StatusCode,
        errorCode = failure.ErrorCode,
        // O corpo integral so e exposto depois da redacao: o cliente ja limpa, mas o
        // executor e a ultima barreira antes do modelo e do historico (FR-027).
        message = Sanitize(failure.Diagnosis),
        responseBody = failure.ResponseBody == null ? null : Sanitize(failure.ResponseBody),
        attemptNumber = _queryAttempts,
        maxAttempts = MaxQueryAttempts,
        queryMayBeCorrected
    };

    private string QueryErrorJson(QueryFailure failure, bool queryMayBeCorrected) =>
        JsonSerializer.Serialize(new { error = BuildDiagnostic(failure, queryMayBeCorrected) });

    private TimeSpan NextDelay(TimeSpan? retryAfter)
    {
        // 429: respeita o tempo indicado pelo servico. 5xx/rede: exponencial com jitter e teto de 30s.
        if (retryAfter.HasValue)
            return retryAfter.Value > TimeSpan.Zero ? retryAfter.Value : TimeSpan.Zero;

        var steps = Math.Max(0, _queryAttempts - 1);
        var ceiling = BackoffBase * Math.Pow(2, Math.Min(steps, 10));
        var capped = ceiling > BackoffCeiling ? BackoffCeiling : ceiling;

        return TimeSpan.FromMilliseconds(capped.TotalMilliseconds * (0.5 + _jitter.NextDouble() * 0.5));
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
            Error = message
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
        sb.AppendLine("Nomes já no formato de referência DAX: copie-os exatamente, com aspas e colchetes.");

        foreach (var table in schema.Tables)
        {
            // Nome com espaco ("public ABIPESCA_COMTRADE") sem aspas parecia "tabela public X"
            // e o modelo descartava o prefixo; a forma citada nao deixa ambiguidade.
            sb.Append("Tabela ").Append(QuoteTable(table.Name));

            var tableDescription = table.UserDescription ?? table.Description;
            if (!string.IsNullOrWhiteSpace(tableDescription))
                sb.Append(" — ").Append(tableDescription);

            sb.AppendLine();

            if (table.Columns.Count > 0)
                sb.Append("  Colunas: ")
                  .AppendLine(string.Join("; ", table.Columns.Select(DescribeColumn)));

            if (table.Measures.Count > 0)
            {
                // Uma por linha: a formula mostra quais filtros a medida ja aplica.
                sb.AppendLine("  Medidas (use como expressão, nunca como coluna de agrupamento):");
                foreach (var measure in table.Measures)
                    sb.Append("    ").AppendLine(DescribeMeasure(measure));
            }
        }

        if (schema.Relationships.Count > 0)
        {
            // Sem isto o modelo nao sabe que filtrar uma dimensao (ex.: SH6) afeta a tabela de fatos.
            sb.AppendLine("Relacionamentos (filtrar a tabela da direita filtra a da esquerda):");
            foreach (var relationship in schema.Relationships)
            {
                sb.Append("  ")
                  .Append(QuoteTable(relationship.FromTable)).Append(QuoteMember(relationship.FromColumn))
                  .Append(" → ")
                  .Append(QuoteTable(relationship.ToTable)).Append(QuoteMember(relationship.ToColumn));

                if (!relationship.IsActive)
                    sb.Append(" (inativo; só vale com USERELATIONSHIP)");

                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static string DescribeColumn(PowerBISchemaColumn column)
    {
        var text = QuoteMember(column.Name);

        if (!string.IsNullOrWhiteSpace(column.DataType))
            text += $" ({column.DataType})";

        var description = column.UserDescription ?? column.Description;
        if (!string.IsNullOrWhiteSpace(description))
            text += $" — {description}";

        if (column.SampleValues is { Count: > 0 })
            text += " — valores: " + string.Join(", ", column.SampleValues.Select(v => $"\"{v.Replace("\"", "\"\"")}\""));

        return text;
    }

    private const int MaxExpressionLength = 400;

    private static string DescribeMeasure(PowerBISchemaMeasure measure)
    {
        var text = QuoteMember(measure.Name);

        var description = measure.UserDescription ?? measure.Description;
        if (!string.IsNullOrWhiteSpace(description))
            text += $" — {description}";

        if (!string.IsNullOrWhiteSpace(measure.Expression))
        {
            // Formula em uma linha e com teto, para nao inflar o contexto com medidas longas.
            var expression = string.Join(" ", measure.Expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (expression.Length > MaxExpressionLength)
                expression = expression[..MaxExpressionLength] + "…";

            text += $" = {expression}";
        }

        return text;
    }

    // Escape DAX: aspa simples dobra dentro de 'Tabela'; colchete de fechamento dobra dentro de [Coluna].
    private static string QuoteTable(string name) => $"'{name.Replace("'", "''")}'";

    private static string QuoteMember(string name) => $"[{name.Replace("]", "]]")}]";

    private string BuildCatalog() =>
        string.Join("\n", _datasets.Select(d =>
            $"- {d.ToolKey}: {d.Name}" + (string.IsNullOrWhiteSpace(d.Description) ? "" : $" — {d.Description}")));

    private string BuildDatasetEnum() =>
        JsonSerializer.Serialize(_datasets.Select(d => d.ToolKey).ToList());

    private static string ErrorJson(string message) =>
        JsonSerializer.Serialize(new Dictionary<string, string> { ["error"] = message });

    private string Sanitize(string message)
    {
        // FR-027: o segredo nao chega ao modelo nem ao historico. Sem truncamento (T007):
        // a mensagem integral e o requisito do diagnostico.
        if (string.IsNullOrEmpty(_credentials.ClientSecret))
            return message;

        return message.Replace(_credentials.ClientSecret, "[redacted]");
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }

    // Uma falha de consulta ja classificada, com tudo que o diagnostico precisa carregar.
    private sealed record QueryFailure(
        string Category,
        PowerBIQueryStatus Status,
        int DurationMs,
        string Diagnosis,
        int? StatusCode,
        string? ErrorCode,
        string? ResponseBody,
        TimeSpan? RetryAfter,
        bool Transient)
    {
        public static QueryFailure Of(
            string category, PowerBIQueryStatus status, int durationMs, string diagnosis,
            int? statusCode, string? errorCode, string? responseBody, TimeSpan? retryAfter, bool transient)
            => new(category, status, durationMs, diagnosis, statusCode, errorCode, responseBody, retryAfter, transient);

        public static QueryFailure OfTimeout(int durationMs, string diagnosis)
            => new(CategoryTransientService, PowerBIQueryStatus.Timeout, durationMs, diagnosis,
                null, null, null, null, Transient: true);

        public static QueryFailure OfLimitReached(string diagnosis)
            => new(CategoryAttemptLimit, PowerBIQueryStatus.Error, 0, diagnosis,
                null, null, null, null, Transient: false);
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
                ErrorMessage = errorMessage == null ? null : Sanitize(errorMessage)
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Falha ao registrar o historico de consulta do Power BI: {Error}", ex.Message);
        }
    }
}
