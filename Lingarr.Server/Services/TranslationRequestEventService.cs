using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class TranslationRequestEventService : ITranslationRequestEventService
{
    private readonly LingarrDbContext _dbContext;

    public TranslationRequestEventService(LingarrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogEvent(int translationRequestId, TranslationStatus status, string? message = null)
    {
        var lastEvent = await _dbContext.TranslationRequestEvents
            .Where(e => e.TranslationRequestId == translationRequestId)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        if (lastEvent != null && lastEvent.Status == status)
        {
            return;
        }

        var translationEvent = new TranslationRequestEvent
        {
            TranslationRequestId = translationRequestId,
            Status = status,
            Message = message
        };

        _dbContext.TranslationRequestEvents.Add(translationEvent);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<TranslationRequestEvent>> GetEvents(int translationRequestId)
    {
        return await _dbContext.TranslationRequestEvents
            .Where(e => e.TranslationRequestId == translationRequestId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }
}
