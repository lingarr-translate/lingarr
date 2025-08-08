using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Services;

public class ProgressService : IProgressService
{
    private readonly IHubContext<TranslationRequestsHub> _hubContext;
    private readonly LingarrDbContext _dbContext;

    public ProgressService(
        IHubContext<TranslationRequestsHub> hubContext, 
        LingarrDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Emit(TranslationRequest translationRequest, int progress)
    {
        await _hubContext.Clients.Group("TranslationRequests").SendAsync("RequestProgress", new
        {
            Id = translationRequest.Id,
            JobId = translationRequest.JobId,
            CompletedAt = translationRequest.CompletedAt,
            Status = translationRequest.Status.GetDisplayName(),
            Progress = progress
        });
    }
}