using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvaBot.API.Auth;
using AvaBot.Application.Services;
using AvaBot.DTO;
using AvaBot.Infra.Interfaces.AppServices;

namespace AvaBot.API.Controllers;

[ApiController]
public class PowerBIController : ControllerBase
{
    private readonly PowerBIService _powerBIService;
    private readonly IValidator<PowerBIConfigUpdateInfo> _configValidator;
    private readonly IValidator<PowerBIDatasetInsertInfo> _datasetValidator;

    public PowerBIController(
        PowerBIService powerBIService,
        IValidator<PowerBIConfigUpdateInfo> configValidator,
        IValidator<PowerBIDatasetInsertInfo> datasetValidator)
    {
        _powerBIService = powerBIService;
        _configValidator = configValidator;
        _datasetValidator = datasetValidator;
    }

    [Authorize]
    [HttpGet("powerbi/{slug}/config")]
    public async Task<IActionResult> GetConfig(string slug)
    {
        try
        {
            var result = await _powerBIService.GetConfigAsync(slug, User.GetUserId());
            return Ok(Result<PowerBIConfigInfo>.Success(result, "Configuracao obtida com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "obter a configuracao");
        }
    }

    [Authorize]
    [HttpPut("powerbi/{slug}/config")]
    public async Task<IActionResult> SaveConfig(string slug, [FromBody] PowerBIConfigUpdateInfo info)
    {
        try
        {
            await _configValidator.ValidateAndThrowAsync(info);
            var result = await _powerBIService.SaveConfigAsync(slug, User.GetUserId(), info);
            return Ok(Result<PowerBIConfigInfo>.Success(result, "Credenciais salvas com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "salvar as credenciais");
        }
    }

    [Authorize]
    [HttpPost("powerbi/{slug}/test")]
    public async Task<IActionResult> TestConnection(string slug)
    {
        try
        {
            var result = await _powerBIService.TestConnectionAsync(slug, User.GetUserId());
            return Ok(Result<PowerBIConnectionTestInfo>.Success(result, "Teste de conexao executado"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "testar a conexao");
        }
    }

    [Authorize]
    [HttpPut("powerbi/{slug}/enabled")]
    public async Task<IActionResult> SetEnabled(string slug, [FromBody] PowerBIEnabledUpdateInfo info)
    {
        try
        {
            var result = await _powerBIService.SetEnabledAsync(slug, User.GetUserId(), info.Enabled);
            return Ok(Result<PowerBIConfigInfo>.Success(result,
                info.Enabled ? "Power BI ativado com sucesso" : "Power BI desativado com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "alterar o status do Power BI");
        }
    }

    [Authorize]
    [HttpGet("powerbi/{slug}/workspaces")]
    public async Task<IActionResult> GetWorkspaces(string slug)
    {
        try
        {
            var result = await _powerBIService.ListWorkspacesAsync(slug, User.GetUserId());
            return Ok(Result<List<PowerBIWorkspaceInfo>>.Success(result, "Workspaces listados com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "listar os workspaces");
        }
    }

    [Authorize]
    [HttpGet("powerbi/{slug}/datasets")]
    public async Task<IActionResult> GetDatasets(string slug)
    {
        try
        {
            var result = await _powerBIService.GetDatasetsAsync(slug, User.GetUserId());
            return Ok(Result<List<PowerBIDatasetInfo>>.Success(result, "Datasets listados com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "listar os datasets");
        }
    }

    [Authorize]
    [HttpPost("powerbi/{slug}/datasets")]
    public async Task<IActionResult> CreateDataset(string slug, [FromBody] PowerBIDatasetInsertInfo info)
    {
        try
        {
            await _datasetValidator.ValidateAndThrowAsync(info);
            var result = await _powerBIService.CreateDatasetAsync(slug, User.GetUserId(), info);
            return Ok(Result<PowerBIDatasetInfo>.Success(result, "Dataset vinculado com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "vincular o dataset");
        }
    }

    [Authorize]
    [HttpPut("powerbi/{slug}/datasets/{id:long}")]
    public async Task<IActionResult> UpdateDataset(string slug, long id, [FromBody] PowerBIDatasetInsertInfo info)
    {
        try
        {
            await _datasetValidator.ValidateAndThrowAsync(info);
            var result = await _powerBIService.UpdateDatasetAsync(slug, User.GetUserId(), id, info);
            return Ok(Result<PowerBIDatasetInfo>.Success(result, "Dataset atualizado com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "atualizar o dataset");
        }
    }

    [Authorize]
    [HttpDelete("powerbi/{slug}/datasets/{id:long}")]
    public async Task<IActionResult> DeleteDataset(string slug, long id)
    {
        try
        {
            var message = await _powerBIService.DeleteDatasetAsync(slug, User.GetUserId(), id);
            return Ok(Result<bool>.Success(true, message));
        }
        catch (Exception ex)
        {
            return Failure(ex, "remover o dataset");
        }
    }

    [Authorize]
    [HttpPost("powerbi/{slug}/datasets/{id:long}/schema/generate")]
    public async Task<IActionResult> GenerateSchema(string slug, long id)
    {
        try
        {
            var result = await _powerBIService.GenerateSchemaAsync(slug, User.GetUserId(), id);
            return Ok(Result<PowerBIDatasetSchemaInfo>.Success(result, "Schema gerado com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "gerar o schema");
        }
    }

    [Authorize]
    [HttpGet("powerbi/{slug}/datasets/{id:long}/schema")]
    public async Task<IActionResult> GetSchema(string slug, long id)
    {
        try
        {
            var result = await _powerBIService.GetSchemaAsync(slug, User.GetUserId(), id);
            return Ok(Result<PowerBIDatasetSchemaInfo>.Success(result, "Schema obtido com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "obter o schema");
        }
    }

    [Authorize]
    [HttpPut("powerbi/{slug}/datasets/{id:long}/schema/descriptions")]
    public async Task<IActionResult> UpdateSchemaDescriptions(string slug, long id, [FromBody] PowerBISchemaDescriptionUpdateInfo info)
    {
        try
        {
            var result = await _powerBIService.UpdateSchemaDescriptionsAsync(slug, User.GetUserId(), id, info);
            return Ok(Result<PowerBIDatasetSchemaInfo>.Success(result, "Descricoes salvas com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "salvar as descricoes");
        }
    }

    [Authorize]
    [HttpGet("powerbi/{slug}/query-logs")]
    public async Task<IActionResult> GetQueryLogs(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _powerBIService.GetQueryLogsAsync(slug, User.GetUserId(), page, pageSize);
            return Ok(Result<PowerBIQueryLogPageInfo>.Success(result, "Historico obtido com sucesso"));
        }
        catch (Exception ex)
        {
            return Failure(ex, "obter o historico");
        }
    }

    private IActionResult Failure(Exception ex, string action) => ex switch
    {
        ValidationException validation => BadRequest(Result<object>.Failure(
            string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
            validation.Errors.Select(e => e.ErrorMessage).ToArray())),
        KeyNotFoundException => NotFound(Result<object>.Failure("Agente nao encontrado")),
        UnauthorizedAccessException => Unauthorized(Result<object>.Failure("Credenciais invalidas")),
        PowerBIApiException apiException => BadRequest(Result<object>.Failure(apiException.Message)),
        ArgumentException argument => BadRequest(Result<object>.Failure(argument.Message)),
        InvalidOperationException invalid => BadRequest(Result<object>.Failure(invalid.Message)),
        _ => StatusCode(500, Result<object>.Failure($"Erro ao {action}: {ex.Message}"))
    };
}
