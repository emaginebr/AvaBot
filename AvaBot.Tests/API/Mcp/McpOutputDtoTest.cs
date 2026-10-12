using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AvaBot.API.Mcp;
using AvaBot.Application.Profiles;
using AvaBot.Domain.Models;
using AvaBot.DTO;

namespace AvaBot.Tests.API.Mcp;

/// <summary>SC-002: nenhum DTO de saida do MCP tem campo de segredo, nem mascarado.</summary>
public class McpOutputDtoTest
{
    // Todos os tipos de McpDTOs.cs e os DTOs do painel reutilizados pelas ferramentas.
    public static IEnumerable<object[]> OutputTypes() =>
    [
        [typeof(McpIdentityInfo)], [typeof(AgentMcpSummaryInfo)], [typeof(AgentMcpInfo)],
        [typeof(AgentMcpCreateInfo)], [typeof(AgentProfileUpdateInfo)],
        [typeof(ChatSessionMcpInfo)], [typeof(ChatMessageMcpInfo)], [typeof(PowerBIMcpInfo)],
        [typeof(TelegramMcpInfo)], [typeof(WhatsappMcpInfo)], [typeof(KnowledgeDocumentMcpInsertInfo)], [typeof(McpMessageInfo)],
        [typeof(PowerBIDatasetInfo)], [typeof(PowerBIConnectionTestInfo)], [typeof(PowerBIWorkspaceInfo)],
        [typeof(PowerBIDatasetSchemaInfo)], [typeof(KnowledgeFileInfo)], [typeof(AgentTestResultInfo)],
        [typeof(PowerBIQueryLogInfo)], [typeof(PowerBIQueryLogPageInfo)]
    ];

    private static readonly string[] ForbiddenFragments = ["secret", "token", "tenant", "clientid", "password", "senha"];

    private static IEnumerable<string> Names(Type type)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            yield return prop.Name;
            var json = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            if (json != null) yield return json;
        }
    }

    [Theory]
    [MemberData(nameof(OutputTypes))]
    public void NoOutputDto_ShouldHaveASecretField(Type type)
    {
        foreach (var name in Names(type))
        {
            Assert.False(McpSecretGuardFilter.ForbiddenKeys.Contains(name), $"{type.Name}.{name} e um campo proibido");

            var lower = name.ToLowerInvariant();
            foreach (var fragment in ForbiddenFragments)
                Assert.False(lower.Contains(fragment), $"{type.Name}.{name} contem '{fragment}'");

            // "apikey" so e aceito como indicador de presenca (hasOpenAIApiKey), nunca como valor.
            if (lower.Contains("apikey"))
                Assert.StartsWith("has", lower);
        }
    }

    [Fact]
    public void AgentMcpInfo_SerializedFromAnAgentWithSecrets_ShouldNotLeakAnyValue()
    {
        var expr = new MapperConfigurationExpression();
        expr.AddProfile<McpProfile>();
        IMapper mapper = new MapperConfiguration(expr, NullLoggerFactory.Instance).CreateMapper();

        var agent = new Agent
        {
            AgentId = 12,
            Name = "Suporte Loja",
            Slug = "suporte-loja",
            SystemPrompt = "prompt",
            ChatModel = "gpt-4o",
            Status = 1,
            OpenAIApiKeyEncrypted = "ENC-sk-live-12345",
            TelegramBotName = "suporte_bot",
            TelegramBotToken = "123456:ABC-TOKEN-TELEGRAM",
            TelegramWebhookSecret = "WEBHOOK-SECRET-XYZ",
            WhatsappToken = "WPP-TOKEN-987",
            PowerBIEnabled = true
        };

        var json = JsonSerializer.Serialize(mapper.Map<AgentMcpInfo>(agent), McpJson.Options);

        Assert.Contains("\"hasOpenAIApiKey\":true", json);
        Assert.Contains("\"telegramBotConfigured\":true", json);
        Assert.Contains("\"whatsappConfigured\":true", json);
        Assert.Contains("\"active\":true", json);
        foreach (var secret in new[] { "ENC-sk-live-12345", "123456:ABC-TOKEN-TELEGRAM", "WEBHOOK-SECRET-XYZ", "WPP-TOKEN-987" })
            Assert.DoesNotContain(secret, json);
        foreach (var key in new[] { "\"openAIApiKey\"", "\"telegramBotToken\"", "\"telegramWebhookSecret\"", "\"whatsappToken\"" })
            Assert.DoesNotContain(key, json);

        var summaryJson = JsonSerializer.Serialize(mapper.Map<AgentMcpSummaryInfo>(agent), McpJson.Options);
        Assert.DoesNotContain("TOKEN", summaryJson);
        Assert.DoesNotContain("systemPrompt", summaryJson);
    }

    [Fact]
    public void PowerBIMcpInfo_Serialized_ShouldNotCarryTenantClientOrSecret()
    {
        var info = new PowerBIMcpInfo
        {
            Enabled = true,
            CredentialConfigured = true,
            LastTestAt = DateTime.UtcNow,
            LastTestSuccess = true,
            LastTestMessage = "ok",
            Datasets = [new PowerBIDatasetInfo { PowerBIDatasetId = 1, Name = "Faturamento", WorkspaceId = "ws", DatasetId = "ds" }]
        };

        var json = JsonSerializer.Serialize(info, McpJson.Options).ToLowerInvariant();

        Assert.Contains("\"credentialconfigured\":true", json);
        Assert.DoesNotContain("tenantid", json);
        Assert.DoesNotContain("clientid", json);
        Assert.DoesNotContain("clientsecret", json);
        Assert.DoesNotContain("secret", json);
    }
}
