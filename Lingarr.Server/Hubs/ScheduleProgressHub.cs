using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Lingarr.Server.Hubs;

public class ScheduleProgressHub : Hub
{
    private readonly LingarrDbContext _dbContext;

    public ScheduleProgressHub(LingarrDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task JoinGroup(GroupRequest request)
    {
        string group = request.Group;
        var translationJob = await _dbContext.TranslationJobs
            .FirstOrDefaultAsync(translationJob => translationJob.JobId == group);

        if (translationJob == null && CheckIfJobExists(request.Group))
        {
            translationJob = new TranslationJob
            {
                JobId = group,
                Completed = false
            };
            _dbContext.TranslationJobs.Add(translationJob);
            await _dbContext.SaveChangesAsync();
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            await Clients.Caller.SendAsync("JoinedGroup", group);
        }
        else
        {
            await Clients.Caller.SendAsync("GroupCompleted", group);
        }
    }

    public async Task LeaveGroup(GroupRequest request)
    {
        string group = request.Group;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
    
    private bool CheckIfJobExists(string jobId)
    {
        using (var connection = JobStorage.Current.GetConnection())
        {
            var job = connection.GetJobData(jobId);
            return job != null;
        }
    }
}