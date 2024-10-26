using Lingarr.Core.Entities;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class SubtitleTranslationService
{
    private int _lastProgression = -1;
    private readonly ITranslationService _translationService;
    private readonly IProgressService? _progressService;
    private readonly ILogger _logger;

    public SubtitleTranslationService(
        ITranslationService translationService,
        ILogger logger,
        IProgressService? progressService = null)
    {
        _translationService = translationService;
        _progressService = progressService;
        _logger = logger;
    }
    
    /// <summary>
    /// Translates a list of subtitle items from the source language to the target language.
    /// </summary>
    /// <param name="subtitles">The list of subtitle items to translate.</param>
    /// <param name="translationRequest">Contains the source and target language specifications.</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation.</param>
    /// <returns>A list of translated subtitle items.</returns>
    public async Task<List<SubtitleItem>> TranslateSubtitles(
        List<SubtitleItem> subtitles,
        TranslationRequest translationRequest,
        CancellationToken cancellationToken)
    {
        if (_progressService == null)
        {
            throw new TranslationException("Subtitle translator could not be initialized, progress service is null.");
        }
        
        int iteration = 0;
        int totalSubtitles = subtitles.Count;

        foreach (var subtitle in subtitles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _lastProgression =  -1;
                break;
            }
            
            for (var index = 0; index < subtitle.Lines.Count; index++)
            {
                subtitle.Lines[index] = await TranslateSubtitleLine(new TranslateAbleSubtitleLine
                {
                    SubtitleLine = subtitle.Lines[index],
                    SourceLanguage = translationRequest.SourceLanguage,
                    TargetLanguage = translationRequest.TargetLanguage
                });
            }

            iteration++;
            int progress = (int)Math.Round((double)iteration * 100 / totalSubtitles);
            
            if (progress != _lastProgression)
            {
                _logger.LogDebug($"Progress: {progress}% (Subtitle {iteration} of {totalSubtitles})");
                await _progressService.Emit(translationRequest, progress, false);
                _lastProgression = progress;
            }
        }

        _lastProgression = -1;
        return subtitles;
    }

    /// <summary>
    /// Translates a single subtitle line using the configured translation service.
    /// </summary>
    /// <param name="translateAbleSubtitle">
    /// Contains the subtitle line to translate along with source and target language specifications.
    /// </param>
    /// <returns>The translated subtitle line.</returns>
    public async Task<string> TranslateSubtitleLine(TranslateAbleSubtitleLine translateAbleSubtitle)
    {
        try
        {
            return await _translationService.TranslateAsync(
                translateAbleSubtitle.SubtitleLine,
                translateAbleSubtitle.SourceLanguage,
                translateAbleSubtitle.TargetLanguage);
        }
        catch (TranslationException ex)
        {
            _logger.LogError(ex, "Translation failed for subtitle line: {SubtitleLine}",
                translateAbleSubtitle.SubtitleLine);
            throw;
        }
    }
}