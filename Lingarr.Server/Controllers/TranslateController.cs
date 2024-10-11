using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Jobs;
using Hangfire;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ISettingService _settings;
    private readonly ILogger<TranslateController> _logger;

    public TranslateController(
        ITranslationServiceFactory translationServiceFactory,
        IBackgroundJobClient backgroundJobClient, 
        ISettingService settings,
            ILogger<TranslateController> logger)
    {
        _translationServiceFactory = translationServiceFactory;
        _backgroundJobClient = backgroundJobClient;
        _settings = settings;
        _logger = logger;
    }
    
    /// <summary>
    /// Initiates a translation job for the provided subtitle data.
    /// </summary>
    /// <param name="translateAbleSubtitle">The subtitle data to be translated. 
    /// This includes the subtitle path, subtitle source language and subtitle target language.</param>
    /// <returns>Returns an HTTP 200 OK response if the job was successfully enqueued.</returns>
    [HttpPost("subtitle")]
    public IActionResult Translate([FromBody] TranslateAbleSubtitle translateAbleSubtitle)
    {
        string jobId = _backgroundJobClient.Enqueue<TranslationJob>(job => 
            job.Execute(null, translateAbleSubtitle, CancellationToken.None)
        );
        return Ok(new { JobId = jobId });
    }
    
    /// <summary>
    /// Translate a single subtitle line
    /// </summary>
    /// <param name="translateAbleSubtitleLine">The subtitle to be translated. 
    /// This includes the subtitle line, subtitle source language and subtitle target language.</param>
    /// <returns>Returns translated string if the translation was successful.</returns>
    [HttpPost("line")]
    public async Task<string> TranslateLine([FromBody] TranslateAbleSubtitleLine translateAbleSubtitleLine)
    {
        var serviceType = await _settings.GetSetting("service_type") ?? "libretranslate";
        
        var translationService = _translationServiceFactory.CreateTranslationService(serviceType);
        var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);

        return await subtitleTranslator.TranslateSubtitleLine(translateAbleSubtitleLine);
    }
}