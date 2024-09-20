using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;

namespace Lingarr.Server.Jobs;

public class TranslateJob
{
    private readonly ITranslateService _translateService;
    private readonly ILogger<TranslateJob> _logger;
    private readonly ProgressService _progressService;

    public TranslateJob(ITranslateService translateService, 
        ProgressService progressService, 
        ILogger<TranslateJob> logger)
    {
        _translateService = translateService;
        _progressService = progressService;
        _logger = logger;
    }

    public async Task Execute(string jobId, TranslateAbleSubtitle translateAbleSubtitle)
    {
        _logger.LogInformation("TranslateJob started for subtitle: {SubtitlePath}", translateAbleSubtitle.SubtitlePath);
        try
        {
            await _translateService.TranslateAsync(jobId,
                translateAbleSubtitle.SubtitlePath,
                translateAbleSubtitle.TargetLanguage,
                _progressService);
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