using System.Text.Json;
using Flurl.Http;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
var inputBasePath = configuration["AgentInput:BasePath"] ?? "../agent_input";

// Resolve relative path from current working directory
var inputPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), inputBasePath));

if (args.Length == 0)
{
    Console.WriteLine("Uso: Avachat.Console <nome-do-agente>");
    Console.WriteLine();
    Console.WriteLine("Estrutura esperada em agent_input/:");
    Console.WriteLine("  system_prompt.md  (obrigatorio)");
    Console.WriteLine("  description.md    (opcional)");
    Console.WriteLine("  docs/             (arquivos .md para base de conhecimento)");
    return 1;
}

var agentName = string.Join(" ", args);

Console.WriteLine($"== Avachat Agent Loader ==");
Console.WriteLine($"API: {baseUrl}");
Console.WriteLine($"Input: {inputPath}");
Console.WriteLine($"Agente: {agentName}");
Console.WriteLine();

// --- Validate input files ---

var systemPromptPath = Path.Combine(inputPath, "system_prompt.md");
if (!File.Exists(systemPromptPath))
{
    Console.WriteLine($"ERRO: Arquivo nao encontrado: {systemPromptPath}");
    return 1;
}

var systemPrompt = (await File.ReadAllTextAsync(systemPromptPath)).Trim();

var descriptionPath = Path.Combine(inputPath, "description.md");
string? description = File.Exists(descriptionPath)
    ? (await File.ReadAllTextAsync(descriptionPath)).Trim()
    : null;

var docsPath = Path.Combine(inputPath, "docs");
var docFiles = Directory.Exists(docsPath)
    ? Directory.GetFiles(docsPath, "*.md")
    : Array.Empty<string>();

Console.WriteLine($"System Prompt: {systemPrompt.Length} caracteres");
Console.WriteLine($"Description: {(description != null ? $"{description.Length} caracteres" : "nenhum")}");
Console.WriteLine($"Documentos: {docFiles.Length} arquivo(s)");
Console.WriteLine();

using var client = new FlurlClient(baseUrl);

// --- Authenticate ---
var username = configuration["ApiSettings:Username"] ?? "admin";
var password = configuration["ApiSettings:Password"] ?? "admin@123";

Console.WriteLine("Autenticando...");
var loginResponse = await client.Request("auth/login").PostJsonAsync(new { username, password });
var loginJson = await loginResponse.GetJsonAsync<JsonElement>();
var token = loginJson.GetProperty("token").GetString();
client.WithHeader("Authorization", $"Bearer {token}");
Console.WriteLine("Autenticado com sucesso!");
Console.WriteLine();

// --- Find or Create Agent ---

// Try to find existing agent by listing all and matching name
var allAgentsResponse = await client.Request("agents").GetAsync();
var allAgents = await allAgentsResponse.GetJsonAsync<ApiResult<List<AgentData>>>();

var existingAgent = allAgents.Dados?.FirstOrDefault(a =>
    string.Equals(a.Name, agentName, StringComparison.OrdinalIgnoreCase));

AgentData agent;

if (existingAgent != null)
{
    Console.WriteLine($"Agente encontrado: ID={existingAgent.AgentId}, Slug={existingAgent.Slug}");
    Console.WriteLine("Atualizando...");

    var updateResponse = await client.Request($"agents/{existingAgent.AgentId}")
        .PutJsonAsync(new
        {
            name = agentName,
            description,
            systemPrompt,
            collectName = existingAgent.CollectName,
            collectEmail = existingAgent.CollectEmail,
            collectPhone = existingAgent.CollectPhone
        });

    var updateResult = await updateResponse.GetJsonAsync<ApiResult<AgentData>>();
    agent = updateResult.Dados!;
    Console.WriteLine($"Agente atualizado: Slug={agent.Slug}");
}
else
{
    Console.WriteLine("Agente nao encontrado. Criando...");

    var createResponse = await client.Request("agents")
        .PostJsonAsync(new
        {
            name = agentName,
            description,
            systemPrompt,
            collectName = false,
            collectEmail = false,
            collectPhone = false
        });

    var createResult = await createResponse.GetJsonAsync<ApiResult<AgentData>>();
    agent = createResult.Dados!;
    Console.WriteLine($"Agente criado: ID={agent.AgentId}, Slug={agent.Slug}");
}

Console.WriteLine();

// --- Sync Knowledge Files ---

if (docFiles.Length > 0)
{
    // Get existing files
    var filesResponse = await client.Request($"files/{agent.AgentId}").GetAsync();
    var existingFiles = await filesResponse.GetJsonAsync<ApiResult<List<FileData>>>();
    var existingFileNames = existingFiles.Dados?.Select(f => f.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase)
        ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var docFile in docFiles)
    {
        var fileName = Path.GetFileName(docFile);

        // Delete existing file with same name to re-upload
        if (existingFileNames.Contains(fileName))
        {
            var toDelete = existingFiles.Dados!.First(f =>
                string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"  Removendo arquivo existente: {fileName} (ID={toDelete.KnowledgeFileId})");
            await client.Request($"files/{agent.AgentId}/{toDelete.KnowledgeFileId}").DeleteAsync();
        }

        Console.WriteLine($"  Enviando: {fileName}");

        var fileBytes = await File.ReadAllBytesAsync(docFile);
        var uploadResponse = await client.Request($"files/{agent.AgentId}")
            .PostMultipartAsync(mp =>
            {
                mp.AddFile("file", new MemoryStream(fileBytes), fileName, "text/markdown");
            });

        var uploadResult = await uploadResponse.GetJsonAsync<ApiResult<FileData>>();
        Console.WriteLine($"  Enviado: {fileName} (ID={uploadResult.Dados!.KnowledgeFileId}, Status={uploadResult.Dados.ProcessingStatus})");
    }

    // Delete files that no longer exist in docs/
    var localFileNames = docFiles.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var orphanFiles = existingFiles.Dados?.Where(f => !localFileNames.Contains(f.FileName)) ?? [];

    foreach (var orphan in orphanFiles)
    {
        Console.WriteLine($"  Removendo arquivo orfao: {orphan.FileName} (ID={orphan.KnowledgeFileId})");
        await client.Request($"files/{agent.AgentId}/{orphan.KnowledgeFileId}").DeleteAsync();
    }
}

Console.WriteLine();
Console.WriteLine("Concluido!");
Console.WriteLine($"  Agente: {agent.Name}");
Console.WriteLine($"  Slug: {agent.Slug}");
Console.WriteLine($"  Chat: {baseUrl}/ws/chat/{agent.Slug}");

return 0;

// --- DTOs ---

class ApiResult<T>
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = "";
    public T? Dados { get; set; }
}

class AgentData
{
    public long AgentId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = "";
    public int Status { get; set; }
    public bool CollectName { get; set; }
    public bool CollectEmail { get; set; }
    public bool CollectPhone { get; set; }
}

class FileData
{
    public long KnowledgeFileId { get; set; }
    public string FileName { get; set; } = "";
    public int ProcessingStatus { get; set; }
}
