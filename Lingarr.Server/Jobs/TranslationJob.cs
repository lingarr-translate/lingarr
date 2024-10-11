using Hangfire;
using Hangfire.Server;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services;

namespace Lingarr.Server.Jobs;

public class TranslationJob
{
    private readonly ILogger<TranslationJob> _logger;
    private readonly ISettingService _settings;
    private readonly IProgressService _progressService;
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly ISubtitleService _subtitleService;

    public TranslationJob(
        ILogger<TranslationJob> logger,
        ISettingService settings,
        IProgressService progressService,
        ITranslationServiceFactory translationServiceFactory,
        ISubtitleService subtitleService)
    {
        _logger = logger;
        _settings = settings;
        _progressService = progressService;
        _translationServiceFactory = translationServiceFactory;
        _subtitleService = subtitleService;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task Execute(
        PerformContext context,
        TranslateAbleSubtitle translateAbleSubtitle,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("TranslateJob started for subtitle: |Green|{filePath}|/Green|",
            translateAbleSubtitle.SubtitlePath);

        string jobId = context.BackgroundJob.Id;
        var serviceType = await _settings.GetSetting("service_type") ?? "libretranslate";

        var translationService = _translationServiceFactory.CreateTranslationService(serviceType);
        var subtitleTranslator = new SubtitleTranslationService(translationService, _logger, _progressService);

        var subtitles = await _subtitleService.ReadSubtitles(translateAbleSubtitle.SubtitlePath);
        var translatedSubtitles =
            await subtitleTranslator.TranslateSubtitles(subtitles, translateAbleSubtitle, jobId, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Translation cancelled for subtitle: {subtitlePath}",
                translateAbleSubtitle.SubtitlePath);
            return;
        }

        var outputPath =
            _subtitleService.CreateFilePath(translateAbleSubtitle.SubtitlePath, translateAbleSubtitle.TargetLanguage);
        await _subtitleService.WriteSubtitles(outputPath, translatedSubtitles);

        _logger.LogInformation("TranslateJob completed and created subtitle: |Green|{filePath}|/Green|", outputPath);
        await _progressService.Emit(jobId, 100, true);
    }
}