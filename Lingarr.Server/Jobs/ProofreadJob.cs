using Hangfire;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Translation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class ProofreadJob
{
    private readonly ILogger<ProofreadJob> _logger;
    private readonly ISettingService _settings;
    private readonly LingarrDbContext _dbContext;
    private readonly IProgressService _progressService;
    private readonly ISubtitleService _subtitleService;
    private readonly IScheduleService _scheduleService;
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly ITranslationRequestService _translationRequestService;
    private readonly ITranslationRequestEventService _eventService;

    public ProofreadJob(
        ILogger<ProofreadJob> logger,
        ISettingService settings,
        LingarrDbContext dbContext,
        IProgressService progressService,
        ISubtitleService subtitleService,
        IScheduleService scheduleService,
        ITranslationServiceFactory translationServiceFactory,
        ITranslationRequestService translationRequestService,
        ITranslationRequestEventService eventService)
    {
        _logger = logger;
        _settings = settings;
        _dbContext = dbContext;
        _progressService = progressService;
        _subtitleService = subtitleService;
        _scheduleService = scheduleService;
        _translationServiceFactory = translationServiceFactory;
        _translationRequestService = translationRequestService;
        _eventService = eventService;
    }

    [AutomaticRetry(Attempts = 0)]
    [Queue("translation")]
    public async Task Execute(
        TranslationRequest translationRequest,
        CancellationToken cancellationToken)
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        var jobId = JobContextFilter.GetCurrentJobId();

        try
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());
            cancellationToken.ThrowIfCancellationRequested();

            var request = await _translationRequestService.UpdateTranslationRequest(translationRequest,
                TranslationStatus.InProgress,
                jobId);
            await _eventService.LogEvent(request.Id, TranslationStatus.InProgress, "Proofread started");
            await _translationRequestService.UpdateActiveCount();
            await _progressService.Emit(request, 0);

            if (string.IsNullOrEmpty(request.TranslatedSubtitle))
            {
                throw new TranslationException(
                    $"Translation request {request.Id} has no translated subtitle to proofread.");
            }

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.ServiceType,
                SettingKeys.Translation.StripSubtitleFormatting
            ]);
            var stripSubtitleFormatting = settings[SettingKeys.Translation.StripSubtitleFormatting] == "true";
            var serviceNames = TranslationServices.Parse(settings[SettingKeys.Translation.ServiceType]);
            var services = _translationServiceFactory.CreateTranslationServices(serviceNames);
            var proofreadEntry = services.FirstOrDefault(entry => entry.Service is IProofreadService);
            if (proofreadEntry.Service is not IProofreadService proofreadService)
            {
                throw new TranslationException(
                    $"No configured translation service supports proofreading: [{string.Join(", ", serviceNames)}]");
            }

            _logger.LogInformation("ProofreadJob started for subtitle: |Green|{filePath}|/Green|",
                request.TranslatedSubtitle);

            var sourceSubtitles = await _subtitleService.ReadSubtitles(request.SubtitleToTranslate);
            var translatedSubtitles = await _subtitleService.ReadSubtitles(request.TranslatedSubtitle);
            var sourceByPosition = sourceSubtitles
                .GroupBy(subtitle => subtitle.Position)
                .ToDictionary(group => group.Key, group => group.First());

            var revised = 0;
            var total = translatedSubtitles.Count;
            var iteration = 0;
            var lastProgress = -1;

            foreach (var subtitle in translatedSubtitles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                iteration++;

                subtitle.TranslatedLines = subtitle.Lines;

                if (sourceByPosition.TryGetValue(subtitle.Position, out var sourceSubtitle))
                {
                    var sourceText = string.Join(" ", ContentLines(sourceSubtitle, stripSubtitleFormatting));
                    var translatedText = string.Join(" ", ContentLines(subtitle, stripSubtitleFormatting));

                    if (!string.IsNullOrWhiteSpace(sourceText) && !string.IsNullOrWhiteSpace(translatedText))
                    {
                        var proofread = await proofreadService.ProofreadAsync(
                            sourceText,
                            translatedText,
                            request.SourceLanguage,
                            request.TargetLanguage,
                            cancellationToken);

                        if (!string.IsNullOrWhiteSpace(proofread)
                            && !string.Equals(proofread.Trim(), translatedText.Trim(), StringComparison.Ordinal))
                        {
                            subtitle.TranslatedLines = [proofread];
                            revised++;

                            await _progressService.EmitProofreadLine(
                                request,
                                subtitle.Position,
                                sourceText,
                                proofread,
                                proofreadEntry.Name);
                        }
                    }
                }

                var progress = (int)Math.Round((double)iteration * 100 / total);
                if (progress != lastProgress)
                {
                    await _progressService.Emit(request, progress);
                    lastProgress = progress;
                }
            }

            await _subtitleService.WriteSubtitles(
                request.TranslatedSubtitle,
                translatedSubtitles,
                stripSubtitleFormatting);

            _logger.LogInformation("ProofreadJob completed and rewrote subtitle: |Green|{filePath}|/Green|",
                request.TranslatedSubtitle);

            await HandleCompletion(jobName, request, revised, total, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            await HandleCancellation(jobName, translationRequest);
        }
        catch (Exception ex)
        {
            translationRequest = await _translationRequestService.UpdateTranslationRequest(translationRequest,
                TranslationStatus.Failed,
                jobId);

            translationRequest.ErrorMessage = ex.Message;
            translationRequest.StackTrace = ex.ToString();
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Failed, ex.Message);
            await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
            await _translationRequestService.UpdateActiveCount();
            await _progressService.Emit(translationRequest, 0);
            throw;
        }
    }

    private static List<string> ContentLines(SubtitleItem subtitle, bool stripSubtitleFormatting)
    {
        return stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines;
    }

    private async Task HandleCompletion(
        string jobName,
        TranslationRequest translationRequest,
        int revised,
        int total,
        CancellationToken cancellationToken)
    {
        translationRequest.CompletedAt = DateTime.UtcNow;
        translationRequest.Status = TranslationStatus.Completed;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Completed,
            $"Proofread completed, {revised} of {total} lines revised");
        await _translationRequestService.UpdateActiveCount();
        await _progressService.Emit(translationRequest, 100);
        await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
    }

    private async Task HandleCancellation(string jobName, TranslationRequest request)
    {
        _logger.LogInformation("Proofread cancelled for subtitle: |Orange|{subtitlePath}|/Orange|",
            request.TranslatedSubtitle);
        var translationRequest =
            await _dbContext.TranslationRequests.FirstOrDefaultAsync(candidate => candidate.Id == request.Id);

        if (translationRequest != null)
        {
            translationRequest.Status = TranslationStatus.Completed;
            translationRequest.ErrorMessage = null;
            await _dbContext.SaveChangesAsync();
            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Cancelled, "Proofread was cancelled");

            await _translationRequestService.UpdateActiveCount();
            await _progressService.Emit(translationRequest, 100);
            await _scheduleService.UpdateJobState(jobName, JobStatus.Cancelled.GetDisplayName());
        }
    }
}
