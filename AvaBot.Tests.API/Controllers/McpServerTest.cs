using System.Text.Json;
using FluentAssertions;
using Flurl.Http;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using AvaBot.Tests.API.Support;

namespace AvaBot.Tests.API.Controllers;

/// <summary>
/// Servidor MCP em /mcp (feature 017), com o cliente do proprio SDK. O setup cria uma chave pelo painel,
/// e o Dispose a revoga. Precisa da API no ar e de um usuario em AvaBot.Tests.API/appsettings.json.
/// </summary>
public class McpServerTest : TestBase
{
    private static readonly string[] SecretFieldNames =
    [
        "openAIApiKey", "removeOpenAIApiKey", "apiKey", "telegramBotToken", "telegramWebhookSecret", "whatsappToken",
        "tenantId", "directoryId", "clientId", "appId", "clientSecret", "secret", "token", "password", "senha"
    ];

    private string _plainKey = string.Empty;
    private long _keyId;
    private McpClient? _client;
    private readonly List<long> _createdAgentIds = new();

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var response = await Api("auth/me/mcp-keys").PostJsonAsync(new { name = "McpServerTest" });
        var body = await response.GetJsonAsync<ApiResult<McpKeyCreatedResponse>>();
        _plainKey = body.Dados!.Key;
        _keyId = body.Dados.AccessKey.McpAccessKeyId;
    }

    public override async Task DisposeAsync()
    {
        if (_client != null) await _client.DisposeAsync();
        foreach (var agentId in _createdAgentIds)
        {
            try { await Api($"agents/{agentId}").DeleteAsync(); } catch { /* ja removido */ }
        }
        try { await Api($"auth/me/mcp-keys/{_keyId}").DeleteAsync(); } catch { /* ja revogada */ }
        await base.DisposeAsync();
    }

    private Uri McpEndpoint => new($"{Settings.BaseUrl.TrimEnd('/')}/mcp");

    private async Task<McpClient> ConnectAsync(string? key = null)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = McpEndpoint,
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {key ?? _plainKey}" }
        });
        return await McpClient.CreateAsync(transport);
    }

    private async Task<McpClient> ClientAsync() => _client ??= await ConnectAsync();

    private static string Text(CallToolResult result) =>
        string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    private static JsonElement Structured(CallToolResult result)
    {
        result.StructuredContent.Should().NotBeNull();
        return result.StructuredContent!.Value;
    }

    private async Task<CallToolResult> CallAsync(string tool, Dictionary<string, object?>? args = null)
    {
        var client = await ClientAsync();
        return await client.CallToolAsync(tool, args);
    }

    /// <summary>POST JSON-RPC bruto em /mcp, para conferir o 401 do contrato sem o cliente do SDK.</summary>
    private async Task<IFlurlResponse> RawToolsListAsync(string? bearer)
    {
        using var client = new FlurlClient(Settings.BaseUrl).AllowAnyHttpStatus();
        var request = client.Request("mcp")
            .WithHeader("Accept", "application/json, text/event-stream");
        if (bearer != null) request = request.WithHeader("Authorization", $"Bearer {bearer}");
        return await request.PostJsonAsync(new { jsonrpc = "2.0", id = 1, method = "tools/list", @params = new { } });
    }

    // ---------- US1: conexao e identidade ----------

    [Fact]
    public async Task Initialize_ShouldAnnounceAvabot_WithInstructionsInPortuguese()
    {
        var client = await ClientAsync();

        client.ServerInfo.Name.Should().Be("avabot");
        client.ServerInstructions.Should().NotBeNullOrEmpty();
        client.ServerInstructions.Should().Contain("nao existe operacao para excluir agente");
    }

    [Fact]
    public async Task ToolsList_ShouldContainQuemSouEu_AndNoDeleteAgentTool_AndNoSecretField()
    {
        var client = await ClientAsync();
        var tools = await client.ListToolsAsync();

        tools.Select(t => t.Name).Should().Contain("quem_sou_eu");
        tools.Select(t => t.Name).Should().NotContain(n =>
            n.Contains("excluir") || n.Contains("delete") || n.Contains("deletar") || n.Contains("remover_agente"));

        foreach (var tool in tools)
        {
            var schema = tool.ProtocolTool.InputSchema.GetRawText();
            foreach (var secret in SecretFieldNames)
                schema.Should().NotContain($"\"{secret}\"", $"a ferramenta {tool.Name} nao pode aceitar {secret}");
        }
    }

    [Fact]
    public async Task QuemSouEu_ShouldReturnTheAccountOfTheKeyOwner()
    {
        var me = await (await Api("auth/me").GetAsync()).GetJsonAsync<ApiResult<JsonElement>>();

        var result = await CallAsync("quem_sou_eu");

        result.IsError.Should().NotBe(true);
        var dados = Structured(result);
        dados.GetProperty("email").GetString().Should().Be(me.Dados.GetProperty("email").GetString());
        dados.GetProperty("name").GetString().Should().Be(me.Dados.GetProperty("name").GetString());
        dados.GetProperty("accessKeyName").GetString().Should().Be("McpServerTest");
        Text(result).Should().Contain("\"email\"");
    }

    [Fact]
    public async Task RevokedKey_ShouldBeRefused_OnTheNextCall()
    {
        var response = await Api("auth/me/mcp-keys").PostJsonAsync(new { name = "Para revogar" });
        var created = (await response.GetJsonAsync<ApiResult<McpKeyCreatedResponse>>()).Dados!;

        var before = await RawToolsListAsync(created.Key);
        before.StatusCode.Should().Be(200);

        await Api($"auth/me/mcp-keys/{created.AccessKey.McpAccessKeyId}").DeleteAsync();

        var after = await RawToolsListAsync(created.Key);
        after.StatusCode.Should().Be(401);
        var body = await after.GetJsonAsync<ApiResult<object>>();
        body.Mensagem.Should().Be("Chave de acesso invalida ou revogada");
    }

    [Fact]
    public async Task TamperedKey_ShouldGetTheSame401_AsARevokedOne()
    {
        var last = _plainKey[^1] == 'A' ? 'B' : 'A';
        var tampered = _plainKey[..^1] + last;

        var response = await RawToolsListAsync(tampered);

        response.StatusCode.Should().Be(401);
        response.Headers.FirstOrDefault("WWW-Authenticate").Should().Contain("Bearer");
        var body = await response.GetJsonAsync<ApiResult<object>>();
        body.Sucesso.Should().BeFalse();
        body.Mensagem.Should().Be("Chave de acesso invalida ou revogada");
    }

    [Fact]
    public async Task PanelJwt_ShouldNotAuthenticateMcp()
    {
        var login = await new FlurlClient(Settings.BaseUrl).Request("auth/login")
            .PostJsonAsync(new { email = Settings.Email, password = Settings.Password });
        var jwt = (await login.GetJsonAsync<JsonElement>()).GetProperty("dados").GetProperty("token").GetString();

        var response = await RawToolsListAsync(jwt);

        response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task MissingHeader_ShouldGet401()
    {
        var response = await RawToolsListAsync(null);

        response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetOnMcp_ShouldBeMethodNotAllowed_InStatelessMode()
    {
        using var client = new FlurlClient(Settings.BaseUrl).AllowAnyHttpStatus();
        var response = await client.Request("mcp")
            .WithHeader("Authorization", $"Bearer {_plainKey}")
            .WithHeader("Accept", "text/event-stream")
            .GetAsync();

        response.StatusCode.Should().Be(405);
    }

    // ---------- US2: agentes ----------

    private async Task<long> CreateAgentViaMcpAsync(string name = "Agente MCP de teste")
    {
        var result = await CallAsync("criar_agente", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["systemPrompt"] = "Voce responde so sobre trocas e devolucoes.",
            ["description"] = "Criado pelo McpServerTest",
            ["collectEmail"] = true
        });
        result.IsError.Should().NotBe(true, Text(result));
        var agentId = Structured(result).GetProperty("agentId").GetInt64();
        _createdAgentIds.Add(agentId);
        return agentId;
    }

    [Fact]
    public async Task AgentLifecycle_CreateListGetUpdateDeactivateReactivate()
    {
        var agentId = await CreateAgentViaMcpAsync("Ciclo MCP");

        var list = await CallAsync("listar_agentes");
        list.IsError.Should().NotBe(true);
        Structured(list).EnumerateArray().Select(a => a.GetProperty("agentId").GetInt64()).Should().Contain(agentId);

        var get = Structured(await CallAsync("obter_agente", new() { ["agentId"] = agentId }));
        get.GetProperty("name").GetString().Should().Be("Ciclo MCP");
        get.GetProperty("hasOpenAIApiKey").GetBoolean().Should().BeFalse();
        get.GetProperty("collectEmail").GetBoolean().Should().BeTrue();
        get.TryGetProperty("telegramBotToken", out _).Should().BeFalse();

        var updated = Structured(await CallAsync("atualizar_agente", new() { ["agentId"] = agentId, ["systemPrompt"] = "Tom mais formal." }));
        updated.GetProperty("systemPrompt").GetString().Should().Be("Tom mais formal.");
        updated.GetProperty("name").GetString().Should().Be("Ciclo MCP");

        var off = Structured(await CallAsync("definir_agente_ativo", new() { ["agentId"] = agentId, ["active"] = false }));
        off.GetProperty("active").GetBoolean().Should().BeFalse();

        var on = Structured(await CallAsync("definir_agente_ativo", new() { ["agentId"] = agentId, ["active"] = true }));
        on.GetProperty("active").GetBoolean().Should().BeTrue();

        // O painel ve o mesmo agente, com os mesmos dados (SC-007).
        var panel = await (await Api($"agents/{get.GetProperty("slug").GetString()}").GetAsync()).GetJsonAsync<ApiResult<AgentResponse>>();
        panel.Dados!.SystemPrompt.Should().Be("Tom mais formal.");
    }

    [Fact]
    public async Task AtualizarAgente_WithASecretField_ShouldBeRefused_AndChangeNothing()
    {
        var agentId = await CreateAgentViaMcpAsync("Guarda de segredos");

        var refused = await CallAsync("atualizar_agente", new()
        {
            ["agentId"] = agentId,
            ["systemPrompt"] = "NAO DEVE SER GRAVADO",
            ["openAIApiKey"] = "sk-teste"
        });

        refused.IsError.Should().BeTrue();
        Text(refused).Should().Contain("Credenciais").And.Contain("'openAIApiKey'").And.Contain("Nenhum campo desta chamada foi gravado");

        var after = Structured(await CallAsync("obter_agente", new() { ["agentId"] = agentId }));
        after.GetProperty("systemPrompt").GetString().Should().NotBe("NAO DEVE SER GRAVADO");
        after.GetProperty("hasOpenAIApiKey").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ObterAgente_WithAnUnknownId_ShouldAnswerNotFound()
    {
        var result = await CallAsync("obter_agente", new() { ["agentId"] = 999999999 });

        result.IsError.Should().BeTrue();
        Text(result).Should().Be("Agente nao encontrado");
    }

    // ---------- US3: logs e teste ----------

    [Fact]
    public async Task ListarSessoes_OfANewAgent_ShouldReturnAnEmptyPage()
    {
        var agentId = await CreateAgentViaMcpAsync("Sem sessoes");

        var result = await CallAsync("listar_sessoes", new() { ["agentId"] = agentId });

        result.IsError.Should().NotBe(true, Text(result));
        var page = Structured(result);
        page.GetProperty("items").GetArrayLength().Should().Be(0);
        page.GetProperty("total").GetInt32().Should().Be(0);
        page.GetProperty("page").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task ListarMensagens_OfAnUnknownSession_ShouldAnswerSessionNotFound()
    {
        var result = await CallAsync("listar_mensagens", new() { ["sessionId"] = 999999999 });

        result.IsError.Should().BeTrue();
        Text(result).Should().Be("Sessao nao encontrada");
    }

    [Fact]
    public async Task ListarConsultasBi_WithErrorFilter_ShouldAnswerWithoutError()
    {
        var agentId = await CreateAgentViaMcpAsync("Logs BI");

        var result = await CallAsync("listar_consultas_bi", new() { ["agentId"] = agentId, ["status"] = "error" });

        result.IsError.Should().NotBe(true, Text(result));
        Structured(result).GetProperty("items").GetArrayLength().Should().Be(0);

        var invalid = await CallAsync("listar_consultas_bi", new() { ["agentId"] = agentId, ["status"] = "falha" });
        invalid.IsError.Should().BeTrue();
        Text(invalid).Should().Be("Status invalido. Use success, error ou timeout");
    }

    [Fact]
    public async Task TestarAgente_ShouldAnswer_OrFailWithAReadableMessage()
    {
        var agentId = await CreateAgentViaMcpAsync("Pergunta de teste");

        var result = await CallAsync("testar_agente", new() { ["agentId"] = agentId, ["question"] = "Voces trocam produtos?" });

        if (result.IsError == true)
        {
            // Sem chave de API no agente nem no servidor: a falha precisa ser legivel, nunca o erro interno generico.
            Text(result).Should().NotContain("Erro interno ao executar");
        }
        else
        {
            Structured(result).TryGetProperty("assistantResponse", out _).Should().BeTrue();
        }

        var invalidHistory = await CallAsync("testar_agente", new()
        {
            ["agentId"] = agentId,
            ["question"] = "Oi",
            ["history"] = new[] { new { role = "system", content = "x" } }
        });
        invalidHistory.IsError.Should().BeTrue();
        Text(invalidHistory).Should().Contain("Historico invalido: item 1 tem role 'system'");
    }

    // ---------- US4: Power BI e canais ----------

    [Fact]
    public async Task ObterPowerBI_OfANewAgent_ShouldHaveNoCredentialAndNoSecretFields()
    {
        var agentId = await CreateAgentViaMcpAsync("Power BI MCP");

        var result = await CallAsync("obter_powerbi", new() { ["agentId"] = agentId });

        result.IsError.Should().NotBe(true, Text(result));
        var info = Structured(result);
        info.GetProperty("credentialConfigured").GetBoolean().Should().BeFalse();
        info.GetProperty("enabled").GetBoolean().Should().BeFalse();
        info.TryGetProperty("tenantId", out _).Should().BeFalse();
        info.TryGetProperty("clientId", out _).Should().BeFalse();
        info.TryGetProperty("clientSecretMasked", out _).Should().BeFalse();

        var enable = await CallAsync("definir_powerbi_ativo", new() { ["agentId"] = agentId, ["enabled"] = true });
        enable.IsError.Should().BeTrue();
        Text(enable).Should().Contain("credencial").And.Contain("painel");

        var tampered = await CallAsync("obter_powerbi", new() { ["agentId"] = agentId, ["tenantId"] = "1234" });
        tampered.IsError.Should().BeTrue();
        Text(tampered).Should().Contain("'tenantId'");
    }

    [Fact]
    public async Task Telegram_WithoutToken_ShouldReportNotConfigured_AndGuideOnWebhook()
    {
        var agentId = await CreateAgentViaMcpAsync("Telegram MCP");

        var status = await CallAsync("obter_telegram", new() { ["agentId"] = agentId });
        status.IsError.Should().NotBe(true, Text(status));
        Structured(status).GetProperty("botConfigured").GetBoolean().Should().BeFalse();
        Text(status).Should().NotContainEquivalentOf("token");

        var webhook = await CallAsync("registrar_webhook_telegram", new() { ["agentId"] = agentId });
        webhook.IsError.Should().BeTrue();
        Text(webhook).Should().Contain("token").And.Contain("painel");
    }

    [Fact]
    public async Task ObterWhatsapp_ShouldAnswer_WithStatusOrAReadableError()
    {
        var agentId = await CreateAgentViaMcpAsync("WhatsApp MCP");

        var result = await CallAsync("obter_whatsapp", new() { ["agentId"] = agentId });

        if (result.IsError == true)
            Text(result).Should().StartWith("Nao foi possivel consultar o WhatsApp");
        else
            Structured(result).TryGetProperty("isConnected", out _).Should().BeTrue();
    }

    // ---------- US5: base de conhecimento ----------

    [Fact]
    public async Task KnowledgeLifecycle_AddListRemove()
    {
        var agentId = await CreateAgentViaMcpAsync("Base MCP");

        var added = await CallAsync("adicionar_documento", new()
        {
            ["agentId"] = agentId,
            ["fileName"] = "politica",
            ["content"] = "# Politica de trocas\n\nTrocas em ate 30 dias com nota fiscal."
        });
        added.IsError.Should().NotBe(true, Text(added));
        var doc = Structured(added);
        doc.GetProperty("fileName").GetString().Should().Be("politica.md");
        doc.TryGetProperty("processingStatus", out _).Should().BeTrue();
        var fileId = doc.GetProperty("knowledgeFileId").GetInt64();

        var list = Structured(await CallAsync("listar_documentos", new() { ["agentId"] = agentId }));
        list.EnumerateArray().Select(d => d.GetProperty("knowledgeFileId").GetInt64()).Should().Contain(fileId);

        var removed = await CallAsync("remover_documento", new() { ["agentId"] = agentId, ["knowledgeFileId"] = fileId });
        removed.IsError.Should().NotBe(true, Text(removed));

        var after = Structured(await CallAsync("listar_documentos", new() { ["agentId"] = agentId }));
        after.GetArrayLength().Should().Be(0);

        var missing = await CallAsync("remover_documento", new() { ["agentId"] = agentId, ["knowledgeFileId"] = 999999999 });
        missing.IsError.Should().BeTrue();
        Text(missing).Should().Be("Documento nao encontrado");
    }

    [Fact]
    public async Task ToolsList_ShouldExposeExactlyTheTwentyEightTools()
    {
        var client = await ClientAsync();
        var tools = await client.ListToolsAsync();

        tools.Select(t => t.Name).Should().BeEquivalentTo(
        [
            "quem_sou_eu",
            "listar_agentes", "obter_agente", "criar_agente", "atualizar_agente", "definir_agente_ativo",
            "listar_sessoes", "listar_mensagens", "listar_consultas_bi", "testar_agente",
            "obter_powerbi", "definir_powerbi_ativo", "testar_conexao_powerbi", "listar_workspaces_powerbi", "listar_datasets_powerbi",
            "vincular_dataset_powerbi", "atualizar_dataset_powerbi", "desvincular_dataset_powerbi",
            "gerar_schema_powerbi", "obter_schema_powerbi", "descrever_schema_powerbi",
            "obter_telegram", "registrar_webhook_telegram", "obter_whatsapp",
            "listar_documentos", "adicionar_documento", "reprocessar_documento", "remover_documento"
        ]);

        var destructive = tools.Where(t => t.ProtocolTool.Annotations?.DestructiveHint == true).Select(t => t.Name);
        destructive.Should().BeEquivalentTo(["desvincular_dataset_powerbi", "remover_documento"]);
    }
}
