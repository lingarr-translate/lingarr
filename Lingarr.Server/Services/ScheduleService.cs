using Hangfire;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;

namespace Lingarr.Server.Services;

public class ScheduleService : IScheduleService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IScheduleService> _logger;

    public ScheduleService(IServiceProvider serviceProvider,
        ILogger<IScheduleService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Initialize()
    {
        using var scope = _serviceProvider.CreateScope();
        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();

        var settings = await settingService.GetSettings(["movie_schedule", "show_schedule", "automation_enabled"]);

        _logger.LogInformation("Configuring media indexers.");
        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case "movie_schedule":
                    RecurringJob.AddOrUpdate<GetMovieJob>(
                        "GetMovieJob",
                        "movies",
                        job => job.Execute(JobCancellationToken.Null),
                        setting.Value);
                    break;
                case "show_schedule":
                    RecurringJob.AddOrUpdate<GetShowJob>(
                        "GetShowJob",
                        "shows",
                        job => job.Execute(JobCancellationToken.Null),
                        setting.Value);
                    break;
                case "automation_enabled":
                    if (setting.Value == "true")
                    {
                        var translationSchedule = await settingService.GetSetting("translation_schedule");
                        RecurringJob.AddOrUpdate<AutomatedTranslationJob>(
                            "AutomatedTranslationJob",
                            job => job.Execute(),
                            translationSchedule,
                            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
                    }
                    break;
            }
        }
        
        RecurringJob.AddOrUpdate<CleanupJob>(
            "CleanupJob",
            job => job.Execute(),
            Cron.Weekly(),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}