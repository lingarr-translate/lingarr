using Hangfire.Server;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Jobs;

public class TranslateJob
{
    private readonly ITranslateService _translateService;
    private readonly ILogger<TranslateJob> _logger;
    private readonly IProgressService _progressService;

    public TranslateJob(ITranslateService translateService, 
        IProgressService progressService, 
        ILogger<TranslateJob> logger)
    {
        _translateService = translateService;
        _progressService = progressService;
        _logger = logger;
        
    }

    public async Task Execute(
        PerformContext context, 
        TranslateAbleSubtitle translateAbleSubtitle,
        CancellationToken cancellationToken)
    {
        string jobId = context.BackgroundJob.Id;
        
        _logger.LogInformation("TranslateJob started for subtitle: {SubtitlePath}", translateAbleSubtitle.SubtitlePath);
        try
        {
            await _translateService.TranslateAsync(
                jobId,
                translateAbleSubtitle.SubtitlePath,
                translateAbleSubtitle.TargetLanguage,
                _progressService,
                cancellationToken);
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