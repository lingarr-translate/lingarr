using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Lingarr.Server.Hubs;

public class SettingUpdatedHub : Hub
{
    private readonly LingarrDbContext _dbContext;

    public SettingUpdatedHub(LingarrDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task JoinGroup(GroupRequest request)
    {
        var group = request.Group;
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Caller.SendAsync("JoinedGroup", group);
    }

    public async Task LeaveGroup(GroupRequest request)
    {
        string group = request.Group;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        await Clients.Caller.SendAsync("LeftGroup", group);
    }
}