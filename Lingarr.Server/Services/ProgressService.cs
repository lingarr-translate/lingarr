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
    private readonly ITranslationRequestService _translationRequestService;

    public ProgressService(
        IHubContext<TranslationRequestsHub> hubContext, 
        LingarrDbContext dbContext,
        ITranslationRequestService translationRequestService)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
        _translationRequestService = translationRequestService;
    }

    /// <inheritdoc />
    public async Task Emit(TranslationRequest translationRequest, int progress)
    {
        if (translationRequest.Status == TranslationStatus.Cancelled || 
            translationRequest.Status == TranslationStatus.Completed)
        {
            await _translationRequestService.UpdateActiveCount();
        }
        
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