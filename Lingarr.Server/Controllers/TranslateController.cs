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

        return await subtitleTranslator.TranslateSubtitleLine(translateAbleSubtitleLine, cancellationToken);
    }

    /// <summary>
    /// Translates subtitle content, supporting both single line and batch translation.
    /// </summary>
    /// <param name="batchTranslationRequest">The translation request containing one or more subtitle items</param>
    /// <param name="cancellationToken">Token to cancel the translation operation</param>
    /// <returns>Translated subtitle content</returns>
    [HttpPost("content")]
    public async Task<ActionResult<BatchTranslatedLine[]>> TranslateContent(
        [FromBody] BatchTranslationRequest batchTranslationRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settings.GetSettings([
                SettingKeys.Translation.UseBatchTranslation,
                SettingKeys.Translation.ServiceType,
                SettingKeys.Translation.MaxBatchSize,
                SettingKeys.Translation.StripSubtitleFormatting
            ]);
            var translationService = _translationServiceFactory.CreateTranslationService(
                settings[SettingKeys.Translation.ServiceType]
            );

            if (settings[SettingKeys.Translation.UseBatchTranslation] == "true"
                && batchTranslationRequest.Lines.Count > 1
                && translationService is IBatchTranslationService batchService)
            {
                _logger.LogInformation("Processing batch translation request with {lineCount} lines from {sourceLanguage} to {targetLanguage}", 
                    batchTranslationRequest.Lines.Count, batchTranslationRequest.SourceLanguage, batchTranslationRequest.TargetLanguage);
                    
                var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);
                var totalSize = batchTranslationRequest.Lines.Count;
                var maxBatchSize = settings[SettingKeys.Translation.MaxBatchSize];
                var stripSubtitleFormatting = settings[SettingKeys.Translation.StripSubtitleFormatting] == "true";
                var maxSize = int.TryParse(maxBatchSize,
                    out var batchSize)
                    ? batchSize
                    : 10000;

                _logger.LogDebug("Batch translation configuration: maxSize={maxSize}, stripFormatting={stripFormatting}, totalLines={totalLines}", 
                    maxSize, stripSubtitleFormatting, totalSize);

                if (maxSize != 0 && totalSize > maxSize)
                {
                    _logger.LogWarning(
                        "Batch size ({Size}) exceeds configured maximum ({Max}). Processing in smaller batches.",
                        totalSize, maxSize);
                    return await ChunkLargeBatch(
                        batchTranslationRequest,
                        translationService,
                        batchService,
                        maxSize,
                        stripSubtitleFormatting,
                        cancellationToken);
                }

                _logger.LogInformation("Processing batch translation within size limits. Converting {lineCount} lines to subtitle items", 
                    batchTranslationRequest.Lines.Count);

                // Convert BatchTranslationRequest items to SubtitleItems for ProcessSubtitleBatch
                var subtitleItems = batchTranslationRequest.Lines.Select(item => new SubtitleItem
                {
                    Position = item.Position,
                    Lines = new List<string> { item.Line },
                    PlaintextLines = new List<string> { item.Line }
                }).ToList();

                _logger.LogDebug("Starting batch subtitle processing with {itemCount} subtitle items", subtitleItems.Count);

                await subtitleTranslator.ProcessSubtitleBatch(
                    subtitleItems,
                    batchService,
                    batchTranslationRequest.SourceLanguage,
                    batchTranslationRequest.TargetLanguage,
                    stripSubtitleFormatting,
                    cancellationToken);

                var results = subtitleItems.Select(subtitle => new BatchTranslatedLine
                {
                    Position = subtitle.Position,
                    Line = string.Join(" ", subtitle.TranslatedLines)
                }).ToArray();

                _logger.LogInformation("Batch translation completed successfully. Processed {resultCount} translated lines", results.Length);

                return Ok(results);
            }
            else
            {
                _logger.LogInformation("Using individual line translation for {lineCount} lines from {sourceLanguage} to {targetLanguage}", 
                    batchTranslationRequest.Lines.Count, 
                    batchTranslationRequest.SourceLanguage, 
                    batchTranslationRequest.TargetLanguage);

                var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);
                var results = new List<BatchTranslatedLine>();

                foreach (var item in batchTranslationRequest.Lines)
                {
                    var translateLine = new TranslateAbleSubtitleLine
                    {
                        SubtitleLine = item.Line,
                        SourceLanguage = batchTranslationRequest.SourceLanguage,
                        TargetLanguage = batchTranslationRequest.TargetLanguage
                    };

                    var translatedText = await subtitleTranslator.TranslateSubtitleLine(
                        translateLine,
                        cancellationToken);

                    results.Add(new BatchTranslatedLine
                    {
                        Position = item.Position,
                        Line = translatedText
                    });
                }

                _logger.LogInformation("Individual line translation completed. Processed {resultCount} lines", results.Count);

                return Ok(results.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating subtitle content");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Processes a large batch by breaking it into smaller batches
    /// </summary>
    private async Task<ActionResult<BatchTranslatedLine[]>> ChunkLargeBatch(
        BatchTranslationRequest batchTranslationRequest,
        ITranslationService translationService,
        IBatchTranslationService batchService,
        int maxBatchSize,
        bool stripSubtitleFormatting,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchTranslatedLine>();
        var currentBatch = new List<SubtitleItem>();
        var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);

        foreach (var item in batchTranslationRequest.Lines)
        {
            if (currentBatch.Count >= maxBatchSize)
            {
                await ProcessBatch(currentBatch, subtitleTranslator, batchService,
                    batchTranslationRequest.SourceLanguage, batchTranslationRequest.TargetLanguage,
                    stripSubtitleFormatting, results, cancellationToken);
                currentBatch.Clear();
            }

            currentBatch.Add(new SubtitleItem
            {
                Position = item.Position,
                Lines =
                [
                    item.Line
                ],
                PlaintextLines =
                [
                    item.Line
                ]
            });
        }

        if (currentBatch.Count > 0)
        {
            await ProcessBatch(currentBatch, subtitleTranslator, batchService,
                batchTranslationRequest.SourceLanguage, batchTranslationRequest.TargetLanguage,
                stripSubtitleFormatting, results, cancellationToken);
        }

        return Ok(results.ToArray());
    }

    /// <summary>
    /// Processes a single batch and adds results to the results collection
    /// </summary>
    private async Task ProcessBatch(
        List<SubtitleItem> batch,
        SubtitleTranslationService subtitleTranslator,
        IBatchTranslationService batchService,
        string sourceLanguage,
        string targetLanguage,
        bool stripSubtitleFormatting,
        List<BatchTranslatedLine> results,
        CancellationToken cancellationToken)
    {
        await subtitleTranslator.ProcessSubtitleBatch(
            batch,
            batchService,
            sourceLanguage,
            targetLanguage,
            stripSubtitleFormatting,
            cancellationToken);

        results.AddRange(batch.Select(subtitle => new BatchTranslatedLine
        {
            Position = subtitle.Position,
            Line = string.Join(" ", subtitle.TranslatedLines ?? subtitle.Lines)
        }));
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
    /// <returns>A list of models in a standardized label/value format for frontend consumption</returns>
    /// <exception cref="InvalidOperationException">Thrown when service is not properly configured or initialization fails</exception>
    [HttpGet("models")]
    public async Task<ActionResult<List<LabelValue>>> GetModels()
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