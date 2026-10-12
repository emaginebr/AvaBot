using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>Ferramentas de conta (contrato mcp-tools.md #1).</summary>
[McpServerToolType]
public sealed class AccountTools
{
    private readonly UserService _userService;
    private readonly ILogger<AccountTools> _logger;

    public AccountTools(UserService userService, ILogger<AccountTools> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [McpServerTool(Name = "quem_sou_eu", Title = "Quem sou eu", ReadOnly = true, Idempotent = true)]
    [Description("Devolve a conta do AvaBot em nome da qual voce esta agindo: id, nome, e-mail e o nome da chave de acesso usada. Nao recebe parametros.")]
    public Task<CallToolResult> QuemSouEu(RequestContext<CallToolRequestParams> context)
        => QuemSouEuAsync(McpCaller.From(context));

    public Task<CallToolResult> QuemSouEuAsync(McpCaller caller) => McpToolResults.RunAsync(_logger, "quem_sou_eu", async () =>
    {
        var user = await _userService.GetAsync(caller.UserId)
            ?? throw new McpException(McpAccessKeyAuthenticationHandler.InvalidKeyMessage);

        return new McpIdentityInfo
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            AccessKeyName = caller.KeyName
        };
    });
}
