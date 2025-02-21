using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class StatisticsJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISubtitleService _subtitleService;
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<StatisticsJob> _logger;

    public StatisticsJob(
        LingarrDbContext dbContext,
        ISubtitleService subtitleService,
        IScheduleService scheduleService,
        ILogger<StatisticsJob> logger)
    {
        _dbContext = dbContext;
        _subtitleService = subtitleService;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [Queue("system")]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        var movieSubtitles = 0;
        var episodeSubtitles = 0;
        var byLanguage = new Dictionary<string, int>();
        var processedPaths = new HashSet<string>();
        await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());
        
        var movies = await _dbContext.Movies.ToListAsync();
        foreach (var movie in movies)
        {
            try 
            {
                var subtitles = await _subtitleService.GetAllSubtitles(movie.Path);
                movieSubtitles += subtitles.Count;
                
                // Group by language for language counts
                foreach (var subtitle in subtitles)
                {
                    if (!byLanguage.ContainsKey(subtitle.Language))
                    {
                        byLanguage[subtitle.Language] = 0;
                    }
                    byLanguage[subtitle.Language]++;
                }
            }
            catch (DirectoryNotFoundException)
            {
                await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
                _logger.LogWarning("Directory not found for movie: {MovieTitle} at path: {Path}", 
                    movie.Title, movie.Path);
            }
        }

        var shows = await _dbContext.Shows
            .Include(s => s.Seasons)
            .ToListAsync();

        foreach (var show in shows)
        {
            foreach (var season in show.Seasons)
            {
                if (string.IsNullOrEmpty(season.Path)) continue;
                
                if (processedPaths.Any(p => season.Path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    var subtitles = await _subtitleService.GetAllSubtitles(season.Path);
                    episodeSubtitles += subtitles.Count;
                    processedPaths.Add(season.Path);
                    
                    // Group by language for language counts
                    foreach (var subtitle in subtitles)
                    {
                        if (!byLanguage.ContainsKey(subtitle.Language))
                        {
                            byLanguage[subtitle.Language] = 0;
                        }
                        byLanguage[subtitle.Language]++;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
                    _logger.LogWarning("Directory not found for season at path: {Path}", season.Path);
                }
            }
        }
        
        // Update statistics
        var stats = await _dbContext.Statistics.SingleOrDefaultAsync();
        if (stats == null)
        {
            stats = new Statistics();
            _dbContext.Statistics.Add(stats);
        }
        stats.TotalEpisodes = await _dbContext.Episodes.CountAsync();
        stats.TotalMovies = movies.Count;
        stats.TotalSubtitles = movieSubtitles + episodeSubtitles;
        stats.SubtitlesByLanguage = byLanguage;
        await _dbContext.SaveChangesAsync();
        await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
    }
}
