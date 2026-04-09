using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Avachat.DTO;
using Avachat.Domain.Models;
using Avachat.Application.Services;
using Avachat.Infra.Interfaces.Repository;
using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.API.Controllers;

[Authorize]
[ApiController]
[Route("files/{agentId:long}")]
public class FileController : ControllerBase
{
    private readonly IKnowledgeFileRepository<KnowledgeFile> _fileRepository;
    private readonly IElasticsearchService _esService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;

    public FileController(
        IKnowledgeFileRepository<KnowledgeFile> fileRepository,
        IElasticsearchService esService,
        IServiceScopeFactory scopeFactory,
        IMapper mapper)
    {
        _fileRepository = fileRepository;
        _esService = esService;
        _scopeFactory = scopeFactory;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetByAgent(long agentId)
    {
        try
        {
            var files = await _fileRepository.GetByAgentIdAsync(agentId);
            var result = _mapper.Map<List<KnowledgeFileInfo>>(files);
            return Ok(Result<List<KnowledgeFileInfo>>.Success(result, "Arquivos listados com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> Upload(long agentId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(Result<object>.Failure("Arquivo nao fornecido"));

            if (!file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                return BadRequest(Result<object>.Failure("Apenas arquivos .md sao aceitos"));

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(Result<object>.Failure("Arquivo excede o limite de 10MB"));

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            var knowledgeFile = new KnowledgeFile
            {
                AgentId = agentId,
                FileName = file.FileName,
                FileContent = content,
                FileSize = file.Length,
                ProcessingStatus = Domain.Enums.ProcessingStatus.Processing
            };

            knowledgeFile = await _fileRepository.CreateAsync(knowledgeFile);

            // Process in background with its own scope
            var fileId = knowledgeFile.KnowledgeFileId;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                await ingestionService.ProcessFileAsync(fileId);
            });

            return Created($"/files/{agentId}/{knowledgeFile.KnowledgeFileId}",
                Result<KnowledgeFileInfo>.Success(_mapper.Map<KnowledgeFileInfo>(knowledgeFile), "Arquivo enviado e em processamento"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpDelete("{fileId:long}")]
    public async Task<IActionResult> Delete(long agentId, long fileId)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.AgentId != agentId)
                return NotFound(Result<object>.Failure("Arquivo nao encontrado"));

            await _esService.DeleteChunksByFileIdAsync(fileId);
            await _fileRepository.DeleteAsync(fileId);

            return Ok(Result<object>.Success(null!, "Arquivo removido com sucesso"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    [HttpPost("{fileId:long}/reprocess")]
    public async Task<IActionResult> Reprocess(long agentId, long fileId)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.AgentId != agentId)
                return NotFound(Result<object>.Failure("Arquivo nao encontrado"));

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
                await ingestionService.ProcessFileAsync(fileId);
            });

            return Ok(Result<object>.Success(null!, "Reprocessamento iniciado"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }
}
