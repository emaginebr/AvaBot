namespace AvaBot.Calibration;

// Espelha appsettings.json (veja appsettings.Example.json). As chaves "Chat:*" e "PowerBI:*"
// sao lidas tambem pelos servicos reais (ChatService, PowerBIToolProvider, PowerBIClient).

public class CalibrationOptions
{
    public OpenAIOptions OpenAI { get; set; } = new();
    public AgentOptions Agent { get; set; } = new();
    public KnowledgeBaseOptions KnowledgeBase { get; set; } = new();
    public PowerBIOptions PowerBI { get; set; } = new();
    public RunOptions Calibration { get; set; } = new();
}

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4.1-mini";
}

public class AgentOptions
{
    public string Name { get; set; } = "Agente";
    public string Slug { get; set; } = "agente";

    /// <summary>Pasta no formato agent_input/&lt;agente&gt;: system_prompt.md e docs/.</summary>
    public string? Folder { get; set; }

    /// <summary>Arquivo do prompt de sistema; padrao: &lt;Folder&gt;/system_prompt.md.</summary>
    public string? SystemPromptFile { get; set; }

    /// <summary>Prompt inline; usado quando nao ha arquivo.</summary>
    public string? SystemPrompt { get; set; }
}

public class KnowledgeBaseOptions
{
    /// <summary>Pasta com os documentos; padrao: &lt;Agent:Folder&gt;/docs.</summary>
    public string? Folder { get; set; }
    public List<string> Extensions { get; set; } = new() { ".md", ".txt" };
    public int ChunkSize { get; set; } = 2000;
    public int ChunkOverlap { get; set; } = 200;
}

public class PowerBIOptions
{
    public bool Enabled { get; set; } = true;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Filtro (nome ou ID) dos workspaces usados quando os datasets sao descobertos.</summary>
    public List<string> Workspaces { get; set; } = new();

    /// <summary>
    /// Opcional. Vazio: todos os datasets acessiveis (filtrados por Workspaces) sao oferecidos.
    /// Um item so com Name tem WorkspaceId/DatasetId descobertos no Power BI.
    /// </summary>
    public List<DatasetOptions> Datasets { get; set; } = new();
}

public class DatasetOptions
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Opcional: gerada a partir do nome, como no painel.</summary>
    public string ToolKey { get; set; } = string.Empty;

    /// <summary>Descricao do catalogo mostrada ao modelo (no painel e escrita pelo administrador).</summary>
    public string? Description { get; set; }

    public string WorkspaceId { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;

    /// <summary>
    /// JSON do schema (mesmo formato salvo pelo painel, aceita userDescription); padrao
    /// calibration/schemas/&lt;ToolKey&gt;.json. Se nao existir, e gerado no Power BI e gravado aqui.
    /// </summary>
    public string? SchemaFile { get; set; }
}

public class RunOptions
{
    public string? Question { get; set; }
    public string? File { get; set; }
    public string? Output { get; set; }
}
