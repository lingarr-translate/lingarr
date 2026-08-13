using Lingarr.Core;
using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Telemetry;

namespace Lingarr.Server.Services;

public class TelemetryService : ITelemetryService
{
    private readonly IStatisticsService _statisticsService;
    private readonly ISettingService _settingService;
    private readonly ILogger<TelemetryService> _logger;
    private readonly ILingarrApiService _lingarrApiService;
    private readonly IWebHostEnvironment _environment;

    public TelemetryService(
        IStatisticsService statisticsService,
        ISettingService settingService,
        ILogger<TelemetryService> logger,
        ILingarrApiService lingarrApiService,
        IWebHostEnvironment environment)
    {
        _statisticsService = statisticsService;
        _settingService = settingService;
        _logger = logger;
        _lingarrApiService = lingarrApiService;
        _environment = environment;
    }

    public async Task<TelemetryPayload> GenerateTelemetryPayload()
    {
        var stats = await _statisticsService.GetStatistics();

        return new TelemetryPayload
        {
            InstallationId = await GetOrCreateInstallationId(),
            Version = LingarrVersion.Number,
            ReportDate = $"{DateTime.UtcNow:yyyy-MM-dd}",
            Platform = LingarrVersion.Platform,
            Metrics = new TelemetryMetrics
            {
                FilesTranslated = stats.TotalFilesTranslated,
                LinesTranslated = stats.TotalLinesTranslated,
                CharactersTranslated = stats.TotalCharactersTranslated,
                ServiceUsage = stats.TranslationsByService,
                LanguagePairs = stats.SubtitlesByLanguage,
                MediaTypeUsage = stats.TranslationsByMediaType,
                ModelUsage = stats.TranslationsByModel
            }
        };
    }

    private async Task<string> GetOrCreateInstallationId()
    {
        var installationId = await _settingService.GetSetting(SettingKeys.Telemetry.InstallationId);
        if (!string.IsNullOrWhiteSpace(installationId))
        {
            return installationId;
        }

        installationId = Guid.NewGuid().ToString("N");
        await _settingService.SetSetting(SettingKeys.Telemetry.InstallationId, installationId);

        return installationId;
    }

    public async Task<bool> CanSubmitTelemetry()
    {
        if (_environment.IsDevelopment())
        {
            return false;
        }

        if (await _settingService.GetSetting(SettingKeys.Telemetry.TelemetryEnabled) != "true")
        {
            return false;
        }

        var lastSubmission = await _settingService.GetSetting(SettingKeys.Telemetry.LastSubmission);
        if (string.IsNullOrEmpty(lastSubmission) || !DateTime.TryParse(lastSubmission, out var lastDate))
        {
            return true;
        }

        return DateTime.UtcNow - lastDate >= TimeSpan.FromDays(7);
    }

    public async Task<bool> SubmitTelemetry(TelemetryPayload payload)
    {
        try
        {
            var success = await _lingarrApiService.SubmitTelemetry(payload);
            if (!success)
            {
                return false;
            }

            await _settingService.SetSetting(SettingKeys.Telemetry.LastSubmission, DateTime.UtcNow.ToString("O"));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting telemetry");
            return false;
        }
    }
}
