using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.DTO;
using AvaBot.API.Auth;
using AvaBot.Application.Services;

namespace AvaBot.API.Controllers;

[Authorize]
[ApiController]
[Route("files/{agentId:long}")]
public class FileController : ControllerBase
{
    private readonly KnowledgeFileService _fileService;
    private readonly IMapper _mapper;

    public FileController(KnowledgeFileService fileService, IMapper mapper)
    {
        _fileService = fileService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetByAgent(long agentId)
    {
        try
        {
            var files = await _fileService.ListAsync(agentId, User.GetUserId());
            var result = _mapper.Map<List<KnowledgeFileInfo>>(files);
            return Ok(Result<List<KnowledgeFileInfo>>.Success(result, "Arquivos listados com sucesso"));
        }
        catch (KeyNotFoundException ex)
        {
            // Agente de outro dono responde como inexistente (FR-011).
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
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

            if (file.Length > KnowledgeFileService.MaxFileSizeBytes)
                return BadRequest(Result<object>.Failure("Arquivo excede o limite de 10MB"));

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            // Dono, criacao e ingestao em segundo plano ficam no KnowledgeFileService (compartilhado com o MCP).
            var knowledgeFile = await _fileService.AddTextAsync(agentId, User.GetUserId(), file.FileName, content);

            return Created($"/files/{agentId}/{knowledgeFile.KnowledgeFileId}",
                Result<KnowledgeFileInfo>.Success(_mapper.Map<KnowledgeFileInfo>(knowledgeFile), "Arquivo enviado e em processamento"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(Result<object>.Failure(ex.Message));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(Result<object>.Failure("Arquivo excede o limite de 10MB"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
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
            await _fileService.DeleteAsync(agentId, User.GetUserId(), fileId);
            return Ok(Result<object>.Success(null!, "Arquivo removido com sucesso"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
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
            await _fileService.ReprocessAsync(agentId, User.GetUserId(), fileId);
            return Ok(Result<object>.Success(null!, "Reprocessamento iniciado"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result<object>.Failure(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(Result<object>.Failure("Credenciais invalidas"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, Result<object>.Failure(ex.Message));
        }
    }
}
