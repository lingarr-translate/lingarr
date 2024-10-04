using Lingarr.Core.Data;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class ProgressService : IProgressService
{
    private readonly IHubContext<ScheduleProgressHub> _hubContext;
    private readonly LingarrDbContext _dbContext;

    public ProgressService(IHubContext<ScheduleProgressHub> hubContext, LingarrDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Emit(string jobId, int progress, bool completed)
    {
        await _hubContext.Clients.Group(jobId).SendAsync("ScheduleProgress", new
        {
            JobId = jobId,
            Progress = progress,
            Completed = completed
        });

        if (completed)
        {
            var translationJob = await _dbContext.TranslationJobs
                .FirstOrDefaultAsync(translationJob => translationJob.JobId == jobId);
            if (translationJob != null)
            {
                translationJob.Completed = true;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}