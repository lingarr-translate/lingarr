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

    public TranslationJob(
        ILogger<TranslationJob> logger, 
        ISettingService settings,
        IProgressService progressService,
        ITranslationServiceFactory translationServiceFactory)
    {
        _logger = logger;
        _settings = settings;
        _progressService = progressService;
        _translationServiceFactory = translationServiceFactory;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task Execute(
        PerformContext context,
        TranslateAbleSubtitle translateAbleSubtitle,
        CancellationToken cancellationToken)
    {
        string jobId = context.BackgroundJob.Id;
        var serviceType = await _settings.GetSetting("service_type") ?? "libretranslate";

        var translationService = _translationServiceFactory.CreateTranslationService(serviceType);
        var subtitleTranslator = new SubtitleTranslator(translationService, _progressService, _logger);

        await subtitleTranslator.TranslateSubtitlesAsync(
            translateAbleSubtitle, 
            jobId, 
            cancellationToken);
    }
}