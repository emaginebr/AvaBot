using Microsoft.AspNetCore.Mvc;
using Avachat.Domain.DTOs;
using Avachat.Domain.Models;
using Avachat.Application.Services;
using Avachat.Infra.Interfaces.Repository;
using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.API.Controllers;

[ApiController]
[Route("api/agents/{agentId:long}/files")]
public class KnowledgeFileController : ControllerBase
{
    private readonly IKnowledgeFileRepository _fileRepository;
    private readonly IElasticsearchService _esService;
    private readonly IngestionService _ingestionService;

    public KnowledgeFileController(
        IKnowledgeFileRepository fileRepository,
        IElasticsearchService esService,
        IngestionService ingestionService)
    {
        _fileRepository = fileRepository;
        _esService = esService;
        _ingestionService = ingestionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetByAgent(long agentId)
    {
        try
        {
            var files = await _fileRepository.GetByAgentIdAsync(agentId);
            var result = files.Select(MapToInfo).ToList();
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

            // Process in background
            _ = Task.Run(async () =>
            {
                await _ingestionService.ProcessFileAsync(knowledgeFile.KnowledgeFileId);
            });

            return Created($"/api/agents/{agentId}/files/{knowledgeFile.KnowledgeFileId}",
                Result<KnowledgeFileInfo>.Success(MapToInfo(knowledgeFile), "Arquivo enviado e em processamento"));
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
                await _ingestionService.ProcessFileAsync(fileId);
            });

            return Ok(Result<object>.Success(null!, "Reprocessamento iniciado"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }

    private static KnowledgeFileInfo MapToInfo(KnowledgeFile f) => new()
    {
        KnowledgeFileId = f.KnowledgeFileId,
        AgentId = f.AgentId,
        FileName = f.FileName,
        FileSize = f.FileSize,
        ProcessingStatus = (int)f.ProcessingStatus,
        ErrorMessage = f.ErrorMessage,
        CreatedAt = f.CreatedAt,
        UpdatedAt = f.UpdatedAt
    };
}
