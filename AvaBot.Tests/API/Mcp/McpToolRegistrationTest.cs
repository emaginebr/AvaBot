using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ModelContextProtocol.Server;
using AvaBot.API.Mcp;
using AvaBot.API.Mcp.Tools;

namespace AvaBot.Tests.API.Mcp;

/// <summary>
/// Reproduz o registro das ferramentas feito em Program.cs: se a geracao de schema de alguma ferramenta
/// falhar, a API nem sobe. Tambem confere nomes, quantidade e anotacoes (contrato mcp-tools.md).
/// </summary>
public class McpToolRegistrationTest
{
    private static readonly string[] ExpectedNames =
    [
        "quem_sou_eu",
        "listar_agentes", "obter_agente", "criar_agente", "atualizar_agente", "definir_agente_ativo",
        "listar_sessoes", "listar_mensagens", "listar_consultas_bi", "testar_agente",
        "obter_powerbi", "definir_powerbi_ativo", "testar_conexao_powerbi", "listar_workspaces_powerbi", "listar_datasets_powerbi",
        "vincular_dataset_powerbi", "atualizar_dataset_powerbi", "desvincular_dataset_powerbi",
        "gerar_schema_powerbi", "obter_schema_powerbi", "descrever_schema_powerbi",
        "obter_telegram", "registrar_webhook_telegram", "obter_whatsapp",
        "listar_documentos", "adicionar_documento", "reprocessar_documento", "remover_documento"
    ];

    private static List<McpServerTool> RegisterAll()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMcpServer(o => { o.ServerInstructions = McpServerInstructions.Text; })
            .WithTools<AccountTools>(McpJson.Options)
            .WithTools<AgentTools>(McpJson.Options)
            .WithTools<ConversationTools>(McpJson.Options)
            .WithTools<PowerBITools>(McpJson.Options)
            .WithTools<ChannelTools>(McpJson.Options)
            .WithTools<KnowledgeTools>(McpJson.Options);

        using var provider = services.BuildServiceProvider();
        return provider.GetServices<McpServerTool>().ToList();
    }

    [Fact]
    public void WithTools_ShouldRegisterExactlyTheTwentyEightTools()
    {
        var tools = RegisterAll();

        Assert.Equal(ExpectedNames.OrderBy(n => n), tools.Select(t => t.ProtocolTool.Name).OrderBy(n => n));
    }

    [Fact]
    public void EveryTool_ShouldHaveDescriptionInputSchema_AndNoSecretField()
    {
        foreach (var tool in RegisterAll())
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.ProtocolTool.Description), $"{tool.ProtocolTool.Name} sem descricao");
            var schema = tool.ProtocolTool.InputSchema.GetRawText();
            Assert.DoesNotContain("RequestContext", schema);
            foreach (var secret in McpSecretGuardFilter.ForbiddenKeys)
                Assert.DoesNotContain($"\"{secret}\"", schema);
        }
    }

    [Fact]
    public void Annotations_ShouldMarkOnlyTheTwoDestructiveTools_AndReadOnlyReads()
    {
        var tools = RegisterAll().ToDictionary(t => t.ProtocolTool.Name);

        var destructive = tools.Values.Where(t => t.ProtocolTool.Annotations?.DestructiveHint == true).Select(t => t.ProtocolTool.Name).OrderBy(n => n);
        Assert.Equal(new[] { "desvincular_dataset_powerbi", "remover_documento" }, destructive);

        foreach (var name in new[] { "quem_sou_eu", "listar_agentes", "obter_agente", "listar_sessoes", "obter_powerbi", "obter_telegram", "listar_documentos" })
            Assert.True(tools[name].ProtocolTool.Annotations?.ReadOnlyHint, $"{name} deveria ser ReadOnly");

        foreach (var name in new[] { "criar_agente", "atualizar_agente", "adicionar_documento", "gerar_schema_powerbi" })
            Assert.NotEqual(true, tools[name].ProtocolTool.Annotations?.ReadOnlyHint);
    }
}
