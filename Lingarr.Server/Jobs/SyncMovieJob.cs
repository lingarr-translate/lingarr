using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Sync;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class SyncMovieJob
{
    private readonly IRadarrService _radarrService;
    private readonly ILogger<SyncMovieJob> _logger;
    private readonly IScheduleService _scheduleService;
    private readonly IMovieSyncService _movieSyncService;
    private readonly LingarrDbContext _dbContext;

    public SyncMovieJob(
        IRadarrService radarrService,
        ILogger<SyncMovieJob> logger,
        IScheduleService scheduleService,
        IMovieSyncService movieSyncService,
        LingarrDbContext dbContext)
    {
        _radarrService = radarrService;
        _logger = logger;
        _scheduleService = scheduleService;
        _movieSyncService = movieSyncService;
        _dbContext = dbContext;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
    [AutomaticRetry(Attempts = 0)]
    [Queue("shows")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        _logger.LogInformation("Radarr sync job initiated");

        try
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

            var movies = await _radarrService.GetMovies();
            if (movies == null) return;

            _logger.LogInformation("Fetched {Count} movies from Radarr", movies.Count());

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            await _movieSyncService.SyncMovies(movies);
            await _movieSyncService.RemoveNonExistentMovies(movies.Select(m => m.Id));

            await transaction.CommitAsync();
            await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
            _logger.LogInformation("Movies synced successfully.");
        }
        catch (Exception ex)
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
            _logger.LogError(ex,
                "An error occurred when syncing movies. Exception details: {ExceptionMessage}, Stack Trace: {StackTrace}",
                ex.Message, ex.StackTrace);
        }
    }
}