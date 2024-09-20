using Lingarr.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class CleanupJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<CleanupJob> _logger;

    public CleanupJob(LingarrDbContext dbContext, 
        ILogger<CleanupJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute()
    {
        var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
        var oldJobs = await _dbContext.TranslationJobs
            .Where(pg => pg.CreatedAt < oneWeekAgo)
            .ToListAsync();

        foreach (var job in oldJobs)
        {
            _dbContext.TranslationJobs.Remove(job);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation($"Removed {oldJobs.Count} translation requests that are older than a week.");
    }
}