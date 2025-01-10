using Lingarr.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace Lingarr.Server.Hubs;

public class TranslationRequestsHub : Hub
{
    public async Task JoinGroup(GroupRequest request)
    {
        string group = request.Group;
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Caller.SendAsync("JoinedGroup", group);
    }

    public async Task LeaveGroup(GroupRequest request)
    {
        string group = request.Group;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}