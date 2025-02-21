using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Sync;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class SyncShowJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISonarrService _sonarrService;
    private readonly ILogger<SyncShowJob> _logger;
    private readonly IScheduleService _scheduleService;
    private readonly IShowSyncService _showSyncService;

    public SyncShowJob(
        LingarrDbContext dbContext,
        ISonarrService sonarrService,
        ILogger<SyncShowJob> logger,
        IScheduleService scheduleService,
        IShowSyncService showSyncService)
    {
        _dbContext = dbContext;
        _sonarrService = sonarrService;
        _logger = logger;
        _scheduleService = scheduleService;
        _showSyncService = showSyncService;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
    [AutomaticRetry(Attempts = 0)]
    [Queue("movies")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        _logger.LogInformation("Sonarr sync job initiated");

        try
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

            var shows = await _sonarrService.GetShows();
            if (shows == null) return;

            _logger.LogInformation("Fetched {ShowCount} shows from Sonarr", shows.Count);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            await _showSyncService.SyncShows(shows);
            await _showSyncService.RemoveNonExistentShows(shows.Select(s => s.Id).ToHashSet());

            await transaction.CommitAsync();
            await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
            _logger.LogInformation("Shows synced successfully.");
        }
        catch (Exception ex)
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
            _logger.LogError(ex,
                "An error occurred when syncing shows. Exception details: {ExceptionMessage}, Stack Trace: {StackTrace}",
                ex.Message, ex.StackTrace);
        }
    }
}