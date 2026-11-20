using Lingarr.Core.Entities;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class TranslationServiceController : ControllerBase
{
    private readonly ITranslationServiceInstanceService _translationServiceInstanceService;

    public TranslationServiceController(ITranslationServiceInstanceService translationServiceInstanceService)
    {
        _translationServiceInstanceService = translationServiceInstanceService;
    }

    /// <summary>
    /// Retrieves the configured translation services in cascade order.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TranslationServiceResponse>>> GetTranslationServices()
    {
        var instances = await _translationServiceInstanceService.GetInstances();
        return Ok(instances.Select(BuildResponse).ToList());
    }

    /// <summary>
    /// Adds a translation service at the end of the cascade.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TranslationServiceResponse>> CreateTranslationService(
        [FromBody] CreateTranslationServiceRequest request)
    {
        try
        {
            var instance = await _translationServiceInstanceService.CreateInstance(request.Provider, request.Name);
            return StatusCode(StatusCodes.Status201Created, BuildResponse(instance));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    /// <summary>
    /// Renames a translation service.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TranslationServiceResponse>> RenameTranslationService(
        int id,
        [FromBody] UpdateTranslationServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name must not be empty" });
        }

        var instance = await _translationServiceInstanceService.RenameInstance(id, request.Name);
        if (instance is null)
        {
            return NotFound();
        }

        return Ok(BuildResponse(instance));
    }

    /// <summary>
    /// Removes a translation service and its settings from the cascade.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTranslationService(int id)
    {
        try
        {
            var deleted = await _translationServiceInstanceService.DeleteInstance(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    /// <summary>
    /// Sets the cascade order from the ids of all translation services.
    /// </summary>
    [HttpPut("order")]
    public async Task<ActionResult<List<TranslationServiceResponse>>> ReorderTranslationServices(
        [FromBody] ReorderTranslationServicesRequest request)
    {
        var instances = await _translationServiceInstanceService.ReorderInstances(request.Ids);
        if (instances is null)
        {
            return BadRequest(new { message = "Ids must contain every translation service exactly once" });
        }

        return Ok(instances.Select(BuildResponse).ToList());
    }

    /// <summary>
    /// Retrieves the settings of a translation service with secret values decrypted.
    /// </summary>
    [HttpGet("{id:int}/settings")]
    public async Task<ActionResult<Dictionary<string, string>>> GetTranslationServiceSettings(int id)
    {
        var settings = await _translationServiceInstanceService.GetInstanceSettings(id);
        if (settings is null)
        {
            return NotFound();
        }

        return Ok(settings);
    }

    /// <summary>
    /// Stores one setting of a translation service.
    /// </summary>
    [HttpPut("{id:int}/settings")]
    public async Task<ActionResult> SetTranslationServiceSetting(
        int id,
        [FromBody] SetTranslationServiceSettingRequest request)
    {
        var instance = await _translationServiceInstanceService.GetInstance(id);
        if (instance is null)
        {
            return NotFound();
        }

        var stored = await _translationServiceInstanceService.SetInstanceSetting(id, request.Key, request.Value);
        if (!stored)
        {
            return BadRequest(new { message = $"Setting '{request.Key}' is not allowed for provider '{instance.Provider}'" });
        }

        return NoContent();
    }

    /// <summary>
    /// Removes the own prompt of a translation service so it uses the global prompt again.
    /// </summary>
    [HttpDelete("{id:int}/settings/{key}")]
    public async Task<ActionResult> DeleteTranslationServicePrompt(int id, string key)
    {
        var instance = await _translationServiceInstanceService.GetInstance(id);
        if (instance is null)
        {
            return NotFound();
        }

        if (!await _translationServiceInstanceService.DeleteInstancePrompt(id, key))
        {
            return BadRequest(new { message = $"Setting '{key}' is not a prompt of provider '{instance.Provider}'" });
        }

        return NoContent();
    }

    private static TranslationServiceResponse BuildResponse(TranslationServiceInstance instance)
    {
        return new TranslationServiceResponse
        {
            Id = instance.Id,
            Provider = instance.Provider,
            Name = instance.Name,
            Position = instance.Position
        };
    }
}
