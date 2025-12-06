using System.Text.Json;
using Lingarr.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Models.Batch.Request;
using Lingarr.Server.Models.Batch.Response;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly ITranslationRequestService _translationRequestService;
    private readonly ISettingService _settings;
    private readonly ILogger<TranslateController> _logger;

    public TranslateController(
        ITranslationServiceFactory translationServiceFactory,
        ITranslationRequestService translationRequestService,
        ISettingService settings,
        ILogger<TranslateController> logger)
    {
        _translationServiceFactory = translationServiceFactory;
        _translationRequestService = translationRequestService;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a translation job for the provided subtitle data.
    /// </summary>
    /// <param name="translateAbleSubtitle">The subtitle data to be translated. 
    /// This includes the subtitle path, subtitle source language and subtitle target language.</param>
    /// <returns>Returns an HTTP 200 OK response if the job was successfully enqueued.</returns>
    [HttpPost("file")]
    public async Task<ActionResult<TranslationJobDto>> Translate([FromBody] TranslateAbleSubtitle translateAbleSubtitle)
    {
        var jobId = await _translationRequestService.CreateRequest(translateAbleSubtitle);
        return Ok(new TranslationJobDto
        {
            JobId = jobId,
        });
    }

    /// <summary>
    /// Translate a single subtitle line
    /// </summary>
    /// <param name="translateAbleSubtitleLine">The subtitle to be translated. 
    /// This includes the subtitle line, subtitle source language and subtitle target language.</param>
    /// <param name="cancellationToken">Token to cancel the translation operation</param>
    /// <returns>Returns translated string if the translation was successful.</returns>
    [HttpPost("line")]
    public async Task<string> TranslateLine(
        [FromBody] TranslateAbleSubtitleLine translateAbleSubtitleLine,
        CancellationToken cancellationToken)
    {
        var serviceType = await _settings.GetSetting(SettingKeys.Translation.ServiceType) ?? "libretranslate";

        var translationService = _translationServiceFactory.CreateTranslationService(serviceType);
        var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);

        if (translateAbleSubtitleLine.SubtitleLine == "")
        {
            return translateAbleSubtitleLine.SubtitleLine;
        }
        return await subtitleTranslator.TranslateSubtitleLine(translateAbleSubtitleLine, cancellationToken);
    }

    /// <summary>
    /// Translates subtitle content, supporting both single line and batch translation.
    /// </summary>
    /// <param name="translateAbleSubtitleContent">The translation request containing one or more subtitle items</param>
    /// <param name="cancellationToken">Token to cancel the translation operation</param>
    /// <returns>Translated subtitle content</returns>
    [HttpPost("content")]
    public async Task<ActionResult<BatchTranslatedLine[]>> TranslateContent(
        [FromBody] TranslateAbleSubtitleContent translateAbleSubtitleContent,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await _translationRequestService.TranslateContentAsync(translateAbleSubtitleContent, cancellationToken);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a list of available source languages and their supported target languages.
    /// </summary>
    /// <returns>A list of source languages, each containing its code, name, and list of supported target language codes</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not properly configured or initialization fails</exception>
    /// <exception cref="JsonException">Thrown when language configuration files cannot be parsed (for file-based services)</exception>
    [HttpGet("languages")]
    public async Task<List<SourceLanguage>> GetLanguages()
    {
        var serviceType = await _settings.GetSetting("service_type") ?? "libretranslate";
        var translationService = _translationServiceFactory.CreateTranslationService(serviceType);

        return await translationService.GetLanguages();
    }

    /// <summary>
    /// Retrieves available AI models for the currently active translation service.
    /// </summary>
    /// <returns>A response containing available models and optional status message</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not properly configured or initialization fails</exception>
    [HttpGet("models")]
    public async Task<ActionResult<ModelsResponse>> GetModels()
    {
        try
        {
            var serviceType = await _settings.GetSetting(SettingKeys.Translation.ServiceType) ?? "libretranslate";
            var translationService = _translationServiceFactory.CreateTranslationService(serviceType);

            // Service-specific logic to get models
            var models = await translationService.GetModels();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for translation service");
            return StatusCode(500, "Failed to retrieve available models");
        }
    }
}
