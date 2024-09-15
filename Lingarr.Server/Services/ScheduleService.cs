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

        var settings = await settingService.GetSettings(["movie_schedule", "show_schedule"]);

        _logger.LogInformation("Configuring media indexers.");
        foreach (var setting in settings)
        {
            var options = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            };
                
            switch (setting.Key)
            {
                case "movie_schedule":
                    RecurringJob.AddOrUpdate<GetMovieJob>(
                        "GetMovieJob", 
                        job => job.Execute(), 
                        setting.Value,
                        options);
                    break;
                case "show_schedule":
                    RecurringJob.AddOrUpdate<GetShowJob>(
                        "GetShowJob", 
                        job => job.Execute(), 
                        setting.Value,
                        options);
                    break;
            }
        }
    }
}