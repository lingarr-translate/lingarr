using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Jobs;

public class TranslateJob
{
    private readonly ITranslateService _translateService;
    private readonly ILogger<TranslateJob> _logger;

    public TranslateJob(ITranslateService translateService, 
        ILogger<TranslateJob> logger)
    {
        _translateService = translateService;
        _logger = logger;
    }

    public async Task Execute(TranslateAbleSubtitle translateAbleSubtitle)
    {
        _logger.LogInformation("TranslateJob started for subtitle: {SubtitlePath}", translateAbleSubtitle.SubtitlePath);
        try
        {
            await _translateService.TranslateAsync(translateAbleSubtitle.SubtitlePath,
                translateAbleSubtitle.TargetLanguage);
        }
        catch (TranslationException ex)
        {
            _logger.LogError(ex, "Translation failed for subtitle {SubtitlePath} due to a translation-specific error.",
                translateAbleSubtitle.SubtitlePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while translating subtitle {SubtitlePath}.",
                translateAbleSubtitle.SubtitlePath);
        }
    }
}