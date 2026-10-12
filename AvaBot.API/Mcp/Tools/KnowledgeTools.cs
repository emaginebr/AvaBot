using System.ComponentModel;
using AutoMapper;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AvaBot.Application.Services;
using AvaBot.DTO;

namespace AvaBot.API.Mcp.Tools;

/// <summary>Base de conhecimento (contrato mcp-tools.md #25–#28). So texto; upload binario fica no painel.</summary>
[McpServerToolType]
public sealed class KnowledgeTools
{
    public const string DocumentNotFoundMessage = "Documento nao encontrado";

    private readonly KnowledgeFileService _fileService;
    private readonly IMapper _mapper;
    private readonly ILogger<KnowledgeTools> _logger;

    public KnowledgeTools(KnowledgeFileService fileService, IMapper mapper, ILogger<KnowledgeTools> logger)
    {
        _fileService = fileService;
        _mapper = mapper;
        _logger = logger;
    }

    // O painel chama o arquivo de "Arquivo"; para o assistente e um documento.
    private static async Task<T> TranslateAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (KnowledgeFileNotFoundException)
        {
            throw new KeyNotFoundException(DocumentNotFoundMessage);
        }
    }

    [McpServerTool(Name = "listar_documentos", Title = "Listar documentos da base", ReadOnly = true, Idempotent = true)]
    [Description("Lista os documentos da base de conhecimento do agente: nome, tamanho em bytes, situacao de processamento (0 processando, 1 pronto, 2 erro), mensagem de erro e datas.")]
    public Task<CallToolResult> ListarDocumentos(RequestContext<CallToolRequestParams> context, [Description("Id do agente.")] long agentId)
        => ListarDocumentosAsync(McpCaller.From(context), agentId);

    public Task<CallToolResult> ListarDocumentosAsync(McpCaller caller, long agentId) => McpToolResults.RunAsync(_logger, "listar_documentos", async () =>
    {
        var files = await _fileService.ListAsync(agentId, caller.UserId);
        return _mapper.Map<List<KnowledgeFileInfo>>(files);
    });

    [McpServerTool(Name = "adicionar_documento", Title = "Adicionar documento a base", ReadOnly = false, Idempotent = false)]
    [Description("Adiciona um documento de texto (Markdown) a base de conhecimento do agente, a partir de um nome e do conteudo. Limite de 10 MB; .md e acrescentado ao nome se faltar. O documento e processado em segundo plano: consulte listar_documentos ate a situacao ficar pronta.")]
    public Task<CallToolResult> AdicionarDocumento(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Nome do documento, por exemplo politica-de-trocas.md.")] string fileName,
        [Description("Conteudo do documento em texto (Markdown), ate 10 MB.")] string content)
        => AdicionarDocumentoAsync(McpCaller.From(context), agentId, new KnowledgeDocumentMcpInsertInfo { FileName = fileName, Content = content });

    public Task<CallToolResult> AdicionarDocumentoAsync(McpCaller caller, long agentId, KnowledgeDocumentMcpInsertInfo info) => McpToolResults.RunAsync(_logger, "adicionar_documento", async () =>
    {
        var file = await _fileService.AddTextAsync(agentId, caller.UserId, info.FileName, info.Content);
        return _mapper.Map<KnowledgeFileInfo>(file);
    });

    [McpServerTool(Name = "reprocessar_documento", Title = "Reprocessar documento", ReadOnly = false, Idempotent = true)]
    [Description("Pede o reprocessamento de um documento da base (util quando a situacao e erro). Volta a 'processando' e roda em segundo plano.")]
    public Task<CallToolResult> ReprocessarDocumento(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do documento (knowledgeFileId).")] long knowledgeFileId)
        => ReprocessarDocumentoAsync(McpCaller.From(context), agentId, knowledgeFileId);

    public Task<CallToolResult> ReprocessarDocumentoAsync(McpCaller caller, long agentId, long knowledgeFileId) => McpToolResults.RunAsync(_logger, "reprocessar_documento", () =>
        TranslateAsync(async () =>
        {
            var file = await _fileService.ReprocessAsync(agentId, caller.UserId, knowledgeFileId);
            return _mapper.Map<KnowledgeFileInfo>(file);
        }));

    [McpServerTool(Name = "remover_documento", Title = "Remover documento da base", ReadOnly = false, Idempotent = false, Destructive = true)]
    [Description("Remove um documento da base de conhecimento do agente; ele deixa de ser usado nas respostas. Nao ha como desfazer.")]
    public Task<CallToolResult> RemoverDocumento(
        RequestContext<CallToolRequestParams> context,
        [Description("Id do agente.")] long agentId,
        [Description("Id do documento (knowledgeFileId).")] long knowledgeFileId)
        => RemoverDocumentoAsync(McpCaller.From(context), agentId, knowledgeFileId);

    public Task<CallToolResult> RemoverDocumentoAsync(McpCaller caller, long agentId, long knowledgeFileId) => McpToolResults.RunAsync(_logger, "remover_documento", () =>
        TranslateAsync(async () =>
        {
            await _fileService.DeleteAsync(agentId, caller.UserId, knowledgeFileId);
            return new McpMessageInfo { Message = "Documento removido" };
        }));
}
