using System.Diagnostics;
using System.Text;
using AvaBot.Application.Services;
using AvaBot.Calibration;
using AvaBot.Calibration.Local;
using AvaBot.Calibration.Report;
using AvaBot.Domain.Enums;
using AvaBot.Domain.Models;
using AvaBot.DTO;
using AvaBot.Infra.AppServices;
using AvaBot.Infra.Interfaces.AppServices;
using AvaBot.Infra.Interfaces.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Calibracao de perguntas: roda o mesmo fluxo do teste de agente (ChatService.TestMessageAsync)
// direto contra a OpenAI e o Power BI, sem API, sem banco e sem Elasticsearch, e imprime o
// relatorio markdown no console. Veja docs/CALIBRATION.md.

const long AgentId = 1;

Console.OutputEncoding = Encoding.UTF8;

CommandLine command;
try
{
    command = CommandLine.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(CommandLine.Usage);
    return 2;
}

if (command.Help)
{
    Console.WriteLine(CommandLine.Usage);
    return 0;
}

var configPath = Paths.Resolve(command.ConfigPath ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Configuração não encontrada: {configPath}");
    Console.Error.WriteLine("Copie AvaBot.Calibration/appsettings.Example.json para AvaBot.Calibration/appsettings.json e preencha.");
    return 2;
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false)
    .AddEnvironmentVariables("AVABOT_CALIBRATION_")
    .Build();

var options = new CalibrationOptions();
configuration.Bind(options);

try
{
    return await RunAsync();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Erro: {ex.Message}");
    return 2;
}
catch (PowerBIApiException ex)
{
    Console.Error.WriteLine($"Erro do Power BI ({ex.StatusCode}): {ex.Message}");
    return 1;
}

async Task<int> RunAsync()
{
    if (command.ListDatasets)
        return await ListDatasetsAsync();

    if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
        throw new InvalidOperationException("OpenAI:ApiKey não configurada.");

    // ---------- Entrada ----------
    var conversations = LoadConversations();

    // ---------- Agente ----------
    var agentFolder = string.IsNullOrWhiteSpace(options.Agent.Folder) ? null : Paths.Resolve(options.Agent.Folder);
    var systemPrompt = LoadSystemPrompt(agentFolder);

    var knowledgeFolder = !string.IsNullOrWhiteSpace(options.KnowledgeBase.Folder)
        ? Paths.Resolve(options.KnowledgeBase.Folder)
        : agentFolder != null ? Path.Combine(agentFolder, "docs") : null;

    var knowledgeBase = LocalKnowledgeBase.Load(
        knowledgeFolder, options.KnowledgeBase.Extensions, options.KnowledgeBase.ChunkSize, options.KnowledgeBase.ChunkOverlap);

    var agent = new Agent
    {
        AgentId = AgentId,
        Name = options.Agent.Name,
        Slug = options.Agent.Slug,
        SystemPrompt = systemPrompt,
        ChatModel = options.OpenAI.ChatModel,
        OpenAIApiKeyEncrypted = options.OpenAI.ApiKey,
        PowerBIEnabled = options.PowerBI.Enabled && HasPowerBICredentials()
    };

    var powerBIConfig = agent.PowerBIEnabled
        ? new AgentPowerBIConfig
        {
            AgentId = AgentId,
            TenantId = options.PowerBI.TenantId,
            ClientId = options.PowerBI.ClientId,
            ClientSecretEncrypted = options.PowerBI.ClientSecret
        }
        : null;

    var datasets = new List<PowerBIDataset>();

    // ---------- Servicos reais com dependencias locais ----------
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();
    services.AddMemoryCache();
    services.AddHttpClient("PowerBI", c => c.DefaultRequestHeaders.Add("Accept", "application/json"));
    services.AddHttpClient("EntraId", c => c.DefaultRequestHeaders.Add("Accept", "application/json"));
    services.AddHttpClient("OpenAI", c => c.Timeout = TimeSpan.FromSeconds(30));

    services.AddSingleton<IAgentRepository<Agent>>(new LocalAgentRepository(agent));
    services.AddSingleton<IAgentPowerBIConfigRepository<AgentPowerBIConfig>>(new LocalPowerBIConfigRepository(powerBIConfig));
    services.AddSingleton<IPowerBIDatasetRepository<PowerBIDataset>>(new LocalPowerBIDatasetRepository(datasets));
    services.AddSingleton<IPowerBIQueryLogRepository<PowerBIQueryLog>, DiscardQueryLogRepository>();
    services.AddSingleton<IChatSessionRepository<ChatSession>, UnusedChatSessionRepository>();
    services.AddSingleton<IChatMessageRepository<ChatMessage>, UnusedChatMessageRepository>();
    services.AddSingleton<ISecretProtector, PlainSecretProtector>();
    services.AddSingleton<IElasticsearchService>(knowledgeBase);

    services.AddSingleton<IPowerBIClient, PowerBIClient>();
    services.AddSingleton<IOpenAIService, OpenAIService>();
    services.AddSingleton<SearchService>();
    services.AddSingleton<PowerBISchemaBuilder>();
    services.AddSingleton<PowerBIToolProvider>();
    services.AddSingleton<ChatService>();

    await using var provider = services.BuildServiceProvider();

    var details = new List<string>
    {
        $"Prompt de sistema: {DescribePromptSource(agentFolder)}",
        knowledgeBase.Folder == null
            ? "Base de conhecimento: nenhuma pasta configurada"
            : Directory.Exists(knowledgeBase.Folder)
                ? $"Base de conhecimento: {knowledgeBase.Folder} ({knowledgeBase.FileCount} arquivo(s), {knowledgeBase.ChunkCount} trecho(s))"
                : $"Base de conhecimento: pasta não encontrada ({knowledgeBase.Folder})"
    };

    if (agent.PowerBIEnabled)
        details.AddRange(await LoadDatasetsAsync(
            provider.GetRequiredService<IPowerBIClient>(), provider.GetRequiredService<PowerBISchemaBuilder>(), datasets));
    else
        details.Add("Power BI: desligado (PowerBI:Enabled = false ou credenciais vazias)");

    // ---------- Execucao ----------
    var chatService = provider.GetRequiredService<ChatService>();
    var run = new CalibrationRun
    {
        AgentSlug = agent.Slug,
        AgentName = agent.Name,
        StartedAt = DateTime.Now,
        Details = details
    };

    var total = Stopwatch.StartNew();

    foreach (var conversationInput in conversations)
        run.Conversations.Add(await RunConversationAsync(chatService, agent, conversationInput));

    total.Stop();
    run.DurationMs = total.ElapsedMilliseconds;

    // ---------- Relatorio ----------
    var report = CalibrationReportBuilder.Build(run);
    var outputPath = Paths.Resolve(command.OutputPath ?? NullIfEmpty(options.Calibration.Output)
        ?? Path.Combine(Path.GetTempPath(), $"avabot-calibration-{DateTime.Now:yyyyMMdd-HHmmss}.md"));

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await File.WriteAllTextAsync(outputPath, report, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    Console.WriteLine(report);
    Console.Error.WriteLine($"Relatório gravado em: {outputPath}");

    // A qualidade da resposta nao reprova; so a impossibilidade de executar.
    return run.Turns.Any(t => t.Result != null) ? 0 : 1;
}

List<ConversationInput> LoadConversations()
{
    var file = command.FilePath ?? (command.Question == null ? NullIfEmpty(options.Calibration.File) : null);
    var question = command.Question ?? (command.FilePath == null ? NullIfEmpty(options.Calibration.Question) : null);

    if (file != null)
    {
        var path = Paths.Resolve(file);
        if (!File.Exists(path))
            throw new InvalidOperationException($"Arquivo de conversas não encontrado: {path}");

        return ConversationFile.Parse(File.ReadAllText(path));
    }

    if (question != null)
        return ConversationFile.FromQuestion(question);

    throw new InvalidOperationException(
        "Informe a pergunta (--question) ou o arquivo de conversas (--file), ou Calibration:Question / Calibration:File no appsettings.json.");
}

string LoadSystemPrompt(string? agentFolder)
{
    var file = !string.IsNullOrWhiteSpace(options.Agent.SystemPromptFile)
        ? Paths.Resolve(options.Agent.SystemPromptFile)
        : agentFolder != null ? Path.Combine(agentFolder, "system_prompt.md") : null;

    if (file != null && File.Exists(file))
        return File.ReadAllText(file);

    if (!string.IsNullOrWhiteSpace(options.Agent.SystemPrompt))
        return options.Agent.SystemPrompt;

    throw new InvalidOperationException(file != null
        ? $"Prompt de sistema não encontrado: {file}"
        : "Configure Agent:Folder, Agent:SystemPromptFile ou Agent:SystemPrompt.");
}

string DescribePromptSource(string? agentFolder)
{
    if (!string.IsNullOrWhiteSpace(options.Agent.SystemPromptFile))
        return Paths.Resolve(options.Agent.SystemPromptFile);

    if (agentFolder != null && File.Exists(Path.Combine(agentFolder, "system_prompt.md")))
        return Path.Combine(agentFolder, "system_prompt.md");

    return "Agent:SystemPrompt (appsettings.json)";
}

bool HasPowerBICredentials() =>
    !string.IsNullOrWhiteSpace(options.PowerBI.TenantId)
    && !string.IsNullOrWhiteSpace(options.PowerBI.ClientId)
    && !string.IsNullOrWhiteSpace(options.PowerBI.ClientSecret);

PowerBICredentials Credentials() => new()
{
    TenantId = options.PowerBI.TenantId,
    ClientId = options.PowerBI.ClientId,
    ClientSecret = options.PowerBI.ClientSecret
};

ServiceProvider PowerBIOnlyProvider()
{
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging();
    services.AddMemoryCache();
    services.AddHttpClient("PowerBI", c => c.DefaultRequestHeaders.Add("Accept", "application/json"));
    services.AddHttpClient("EntraId", c => c.DefaultRequestHeaders.Add("Accept", "application/json"));
    services.AddSingleton<IPowerBIClient, PowerBIClient>();
    return services.BuildServiceProvider();
}

// --list-datasets: o que o aplicativo enxerga no Power BI, para preencher (ou filtrar) a configuracao.
async Task<int> ListDatasetsAsync()
{
    if (!HasPowerBICredentials())
        throw new InvalidOperationException("Preencha PowerBI:TenantId, ClientId e ClientSecret.");

    await using var provider = PowerBIOnlyProvider();
    var workspaces = await provider.GetRequiredService<IPowerBIClient>().ListWorkspacesWithDatasetsAsync(Credentials());

    Console.WriteLine(DatasetCatalog.Describe(workspaces));
    return 0;
}

// Datasets: da configuracao ou descobertos no Power BI; schema de arquivo local ou gerado.
async Task<List<string>> LoadDatasetsAsync(IPowerBIClient client, PowerBISchemaBuilder builder, List<PowerBIDataset> target)
{
    var lines = new List<string>();
    var credentials = Credentials();
    List<DatasetOptions> sources;

    try
    {
        if (DatasetCatalog.NeedsDiscovery(options.PowerBI))
        {
            Console.Error.WriteLine("Listando workspaces e datasets no Power BI...");
            var workspaces = await client.ListWorkspacesWithDatasetsAsync(credentials);
            sources = DatasetCatalog.Resolve(options.PowerBI, workspaces);
            lines.Add($"Datasets descobertos no Power BI: {sources.Count}" +
                (options.PowerBI.Workspaces.Count > 0 ? $" (workspaces: {string.Join(", ", options.PowerBI.Workspaces)})" : ""));
        }
        else
        {
            sources = DatasetCatalog.Resolve(options.PowerBI, Array.Empty<PowerBIWorkspace>());
        }
    }
    catch (Exception ex) when (ex is PowerBIApiException or HttpRequestException or TaskCanceledException)
    {
        lines.Add($"Power BI: não foi possível listar os datasets ({ex.Message}); nenhum dataset foi oferecido ao modelo");
        return lines;
    }

    for (var i = 0; i < sources.Count; i++)
    {
        var source = sources[i];
        var schemaFile = Paths.Resolve(string.IsNullOrWhiteSpace(source.SchemaFile)
            ? Path.Combine("calibration", "schemas", $"{source.ToolKey}.json")
            : source.SchemaFile);
        var previous = schemaFile != null && File.Exists(schemaFile)
            ? PowerBISchema.Deserialize(File.ReadAllText(schemaFile))
            : null;

        PowerBISchema? schema = command.RefreshSchema ? null : previous;
        string origin;

        if (schema != null)
        {
            origin = $"schema de {schemaFile}";
        }
        else
        {
            try
            {
                Console.Error.WriteLine($"Gerando schema de '{source.Name}' no Power BI...");
                var (generated, status) = await builder.BuildAsync(credentials, source.WorkspaceId, source.DatasetId);

                // Ao regerar, as descricoes escritas no arquivo sao preservadas (mesma regra do painel).
                schema = previous != null ? PowerBISchema.MergeUserDescriptions(previous, generated) : generated;
                origin = $"schema gerado no Power BI ({status})";

                if (schemaFile != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(schemaFile)!);
                    await File.WriteAllTextAsync(schemaFile, FormatJson(PowerBISchema.Serialize(schema)));
                    origin += $", salvo em {schemaFile}";
                }
            }
            catch (Exception ex) when (ex is PowerBIApiException or HttpRequestException or TaskCanceledException)
            {
                lines.Add($"Dataset {source.Name}: schema indisponível ({ex.Message}); o dataset não foi oferecido ao modelo");
                continue;
            }
        }

        target.Add(new PowerBIDataset
        {
            PowerBIDatasetId = i + 1,
            AgentId = AgentId,
            Name = source.Name,
            ToolKey = source.ToolKey,
            Description = source.Description,
            WorkspaceId = source.WorkspaceId,
            DatasetId = source.DatasetId,
            SchemaJson = PowerBISchema.Serialize(schema),
            SchemaStatus = PowerBISchemaStatus.Generated
        });

        lines.Add($"Dataset {source.Name} (`{source.ToolKey}`): {origin}");
    }

    return lines;
}

async Task<CalibrationConversation> RunConversationAsync(ChatService chatService, Agent agent, ConversationInput input)
{
    var conversation = new CalibrationConversation { Name = input.Name };
    var history = new List<AgentTestMessageInfo>();
    string? failure = null;

    for (var i = 0; i < input.Messages.Count; i++)
    {
        var turn = new CalibrationTurn { Index = i + 1, Question = input.Messages[i], History = history.ToList() };
        conversation.Turns.Add(turn);

        // Sem a resposta anterior o historico ficaria incompleto (FR-015).
        if (failure != null)
        {
            turn.Status = CalibrationTurnStatus.NaoEnviada;
            turn.Reason = failure;
            continue;
        }

        Console.Error.WriteLine($"[{input.Name}] mensagem {turn.Index}/{input.Messages.Count}: {turn.Question}");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            turn.Result = await chatService.TestMessageAsync(
                agent.AgentId, agent.ChatModel, agent.SystemPrompt, turn.Question, turn.History);
            turn.Status = CalibrationTurnStatus.Concluida;

            history.Add(new AgentTestMessageInfo { Role = "user", Content = turn.Question });
            history.Add(new AgentTestMessageInfo { Role = "assistant", Content = turn.Result.AssistantResponse });
        }
        catch (AgentTestFailedException ex)
        {
            turn.Result = ex.PartialResult;
            turn.Status = CalibrationTurnStatus.Falhou;
            turn.Reason = ex.Message;
        }
        catch (Exception ex)
        {
            turn.Status = CalibrationTurnStatus.Falhou;
            turn.Reason = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            turn.ClientDurationMs = stopwatch.ElapsedMilliseconds;
        }

        if (turn.Status == CalibrationTurnStatus.Falhou)
            failure = $"falha na mensagem {turn.Index}";
    }

    return conversation;
}

static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

static string FormatJson(string json)
{
    using var document = System.Text.Json.JsonDocument.Parse(json);
    return System.Text.Json.JsonSerializer.Serialize(document.RootElement, new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}
