using Hangfire;
using Hangfire.Storage;
using Lingarr.Core.Configuration;
using Lingarr.Core.Enum;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Lingarr.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Services;

public class ScheduleService : IScheduleService
{
    private readonly IHubContext<JobProgressHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IScheduleService> _logger;

    public ScheduleService(
        IHubContext<JobProgressHub> hubContext,
        IServiceProvider serviceProvider,
        ILogger<IScheduleService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Initialize()
    {
        using var scope = _serviceProvider.CreateScope();
        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
        var translationRequestService = scope.ServiceProvider.GetRequiredService<ITranslationRequestService>();

        var settings = await settingService.GetSettings([
            SettingKeys.Automation.MovieSchedule,
            SettingKeys.Automation.ShowSchedule,
            SettingKeys.Automation.AutomationEnabled,
            SettingKeys.Telemetry.TelemetryEnabled
        ]);

        _logger.LogInformation("Configuring media indexers.");
        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case "movie_schedule":
                    RecurringJob.AddOrUpdate<SyncMovieJob>(
                        "SyncMovieJob",
                        job => job.Execute(),
                        setting.Value);
                    break;
                case "show_schedule":
                    RecurringJob.AddOrUpdate<SyncShowJob>(
                        "SyncShowJob",
                        job => job.Execute(),
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
                case "telemetry_enabled":
                    if (setting.Value == "true")
                    {
                        RecurringJob.AddOrUpdate<TelemetryJob>(
                            "TelemetryJob",
                            job => job.Execute(),
                            "0 9 * * 5",
                            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
                    }
                    break;
            }
        }

        RecurringJob.AddOrUpdate<CleanupJob>(
            "CleanupJob",
            job => job.Execute(),
            Cron.Weekly,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        RecurringJob.AddOrUpdate<StatisticsJob>(
            "StatisticsJob",
            job => job.Execute(),
            Cron.Daily,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        _logger.LogInformation("Starting pending translation requests.");
        await translationRequestService.ResumeTranslationRequests();
    }

    public List<RecurringJobStatus> GetRecurringJobs()
    {
        var monitor = JobStorage.Current.GetMonitoringApi();
        var recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();

        return recurringJobs
            .Select(job => MapToJobStatus(job, monitor))
            .OrderBy(j => j.Id)
            .ToList();
    }

    public string GetJobState(string jobId)
    {
        var monitor = JobStorage.Current.GetMonitoringApi();

        // Check each possible state
        if (monitor.SucceededJobs(0, 1).Any(j => j.Key == jobId))
            return JobStatus.Succeeded.GetDisplayName();
        if (monitor.FailedJobs(0, 1).Any(j => j.Key == jobId))
            return JobStatus.Failed.GetDisplayName();
        if (monitor.ScheduledJobs(0, 1).Any(j => j.Key == jobId))
            return JobStatus.Scheduled.GetDisplayName();
        if (monitor.EnqueuedJobs("default", 0, 1).Any(j => j.Key == jobId))
            return JobStatus.Enqueued.GetDisplayName();

        return JobStatus.Planned.GetDisplayName();
    }

    public async Task UpdateJobState(string jobId, string state)
    {
        try
        {
            await _hubContext.Clients.Group("JobProgress")
                .SendAsync("JobStateUpdated", jobId, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job state for job {JobId}", jobId);
            throw;
        }
    }

    private RecurringJobStatus MapToJobStatus(RecurringJobDto dto, IMonitoringApi monitor)
    {
        var status = new RecurringJobStatus
        {
            Id = dto.Id,
            Cron = dto.Cron,
            Queue = dto.Queue,
            JobMethod = dto.Job?.Method?.Name ?? string.Empty,
            NextExecution = dto.NextExecution,
            LastJobId = dto.LastJobId,
            LastJobState = dto.LastJobState,
            LastExecution = dto.LastExecution,
            CreatedAt = dto.CreatedAt,
            TimeZoneId = dto.TimeZoneId
        };

        // Check if there's a currently running job for this recurring job
        if (!string.IsNullOrEmpty(dto.LastJobId))
        {
            var processingJobs = monitor.ProcessingJobs(0, int.MaxValue);
            var currentJob = processingJobs.FirstOrDefault(j =>
                j.Key == dto.LastJobId ||
                (j.Value?.Job?.Args?.Contains(dto.Id) ?? false));

            if (currentJob.Value != null)
            {
                status.IsCurrentlyRunning = true;
                status.CurrentState = "Processing";
                status.CurrentJobId = currentJob.Key;
            }
            else
            {
                // Check other states if not processing
                status.CurrentState = GetJobState(dto.LastJobId);
            }
        }

        return status;
    }
}