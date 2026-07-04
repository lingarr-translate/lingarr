using Lingarr.Contracts.Models;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
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
            translationRequest.Id,
            translationRequest.JobId,
            translationRequest.CompletedAt,
            translationRequest.ErrorMessage,
            translationRequest.StackTrace,
            Status = translationRequest.Status.GetDisplayName(),
            Progress = progress
        });
    }

    /// <inheritdoc />
    public async Task EmitLine(
        TranslationRequest request,
        int position,
        string source,
        string target,
        string? service = null,
        LanguagePair? pair = null)
    {
        var fallback = GetFallbackPair(pair);
        _dbContext.TranslationRequestLines.Add(new TranslationRequestLine
        {
            TranslationRequestId = request.Id,
            Position = position,
            Source = source,
            Target = target,
            Service = service,
            MatchedSource = fallback?.Source,
            MatchedTarget = fallback?.Target,
            Tier = fallback?.Tier
        });
        await _dbContext.SaveChangesAsync();

        await _hubContext.Clients.Group("TranslationRequests").SendAsync("LineTranslated", new
        {
            request.Id,
            Position = position,
            Source = source,
            Target = target,
            Service = service,
            MatchedSource = fallback?.Source,
            MatchedTarget = fallback?.Target,
            Tier = fallback?.Tier.ToString()
        });
    }

    /// <inheritdoc />
    public async Task EmitLines(TranslationRequest request, List<TranslatedLineData> lines)
    {
        foreach (var line in lines)
        {
            var fallback = GetFallbackPair(line.Pair);
            _dbContext.TranslationRequestLines.Add(new TranslationRequestLine
            {
                TranslationRequestId = request.Id,
                Position = line.Position,
                Source = line.Source,
                Target = line.Target,
                Service = line.Service,
                MatchedSource = fallback?.Source,
                MatchedTarget = fallback?.Target,
                Tier = fallback?.Tier
            });
        }
        await _dbContext.SaveChangesAsync();

        foreach (var line in lines)
        {
            var fallback = GetFallbackPair(line.Pair);
            await _hubContext.Clients.Group("TranslationRequests").SendAsync("LineTranslated", new
            {
                request.Id,
                line.Position,
                line.Source,
                line.Target,
                line.Service,
                MatchedSource = fallback?.Source,
                MatchedTarget = fallback?.Target,
                Tier = fallback?.Tier.ToString()
            });
        }
    }

    /// <summary>
    /// Returns the pair when the service translated against a fallback variant of the requested
    /// codes, or null when the match was exact (so no fallback columns need to be persisted).
    /// </summary>
    private static LanguagePair? GetFallbackPair(LanguagePair? pair)
    {
        if (pair == null || pair.Tier == MatchTier.Exact)
        {
            return null;
        }
        return pair;
    }
}