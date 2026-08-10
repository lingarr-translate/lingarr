using DeepL;
using Hangfire;
using Lingarr.Contracts.Exceptions;
using Lingarr.Contracts.Models;
using Lingarr.Contracts.Translation;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Core.Configuration;
using Lingarr.Server.Hubs;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Jobs;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Models.Batch.Response;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Models.TranslationRequests;
using Lingarr.Server.Services.Translation;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services;

public class TranslationRequestService : ITranslationRequestService
{
    private readonly LingarrDbContext _dbContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHubContext<TranslationRequestsHub> _hubContext;
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly IProgressService _progressService;
    private readonly IStatisticsService _statisticsService;
    private readonly IMediaService _mediaService;
    private readonly ISettingService _settingService;
    private readonly ISubtitleService _subtitleService;
    private readonly ITranslationRequestEventService _eventService;
    private readonly ILogger<TranslationRequestService> _logger;
    private const string ManualService = "manual";
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> _asyncTranslationJobs = new();

    public TranslationRequestService(
        LingarrDbContext dbContext,
        IBackgroundJobClient backgroundJobClient,
        IHubContext<TranslationRequestsHub> hubContext,
        ITranslationServiceFactory translationServiceFactory,
        IProgressService progressService,
        IStatisticsService statisticsService,
        IMediaService mediaService,
        ISettingService settingService,
        ISubtitleService subtitleService,
        ITranslationRequestEventService eventService,
        ILogger<TranslationRequestService> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _backgroundJobClient = backgroundJobClient;
        _translationServiceFactory = translationServiceFactory;
        _progressService = progressService;
        _statisticsService = statisticsService;
        _mediaService = mediaService;
        _settingService = settingService;
        _subtitleService = subtitleService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TranslationRequestDetail?> GetTranslationRequest(int id)
    {
        var request = await _dbContext.TranslationRequests.FindAsync(id);
        if (request == null)
        {
            return null;
        }

        var events = await _eventService.GetEvents(id);

        var translationRequestLines = await _dbContext.TranslationRequestLines
            .Where(translationRequestLine => translationRequestLine.TranslationRequestId == id)
            .OrderBy(translationRequestLine => translationRequestLine.Position)
            .Select(translationRequestLine => new TranslationRequestSubtitleLines
            {
                Position = translationRequestLine.Position,
                Source = translationRequestLine.Source,
                Target = translationRequestLine.Target,
                Service = translationRequestLine.Service
            })
            .ToListAsync();
        
        return new TranslationRequestDetail
        {
            Id = request.Id,
            JobId = request.JobId,
            MediaId = request.MediaId,
            Title = request.Title,
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            SubtitleToTranslate = request.SubtitleToTranslate,
            TranslatedSubtitle = request.TranslatedSubtitle,
            MediaType = request.MediaType,
            Status = request.Status,
            CompletedAt = request.CompletedAt,
            ErrorMessage = request.ErrorMessage,
            StackTrace = request.StackTrace,
            Progress = request.Status == TranslationStatus.Completed ? 100 : 0,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            Events = events.Select(translationRequestEvent => new TranslationRequestEventDetail
            {
                Id = translationRequestEvent.Id,
                Status = translationRequestEvent.Status,
                Message = translationRequestEvent.Message,
                CreatedAt = translationRequestEvent.CreatedAt
            }).ToList(),
            Lines = translationRequestLines.Count > 0 ? translationRequestLines : []
        };
    }

    /// <inheritdoc />
    public async Task<int> CreateRequest(TranslateAbleSubtitle translateAbleSubtitle)
    {
        var mediaTitle = await FormatMediaTitle(translateAbleSubtitle.MediaId, translateAbleSubtitle.MediaType);
        var translationRequest = new TranslationRequest
        {
            MediaId = translateAbleSubtitle.MediaId,
            Title = mediaTitle,
            SourceLanguage = translateAbleSubtitle.SourceLanguage,
            TargetLanguage = translateAbleSubtitle.TargetLanguage,
            SubtitleToTranslate = translateAbleSubtitle.SubtitlePath,
            MediaType = translateAbleSubtitle.MediaType,
            Status = TranslationStatus.Pending
        };

        return await EnqueueRequest(translationRequest);
    }

    private async Task<int> EnqueueRequest(TranslationRequest translationRequest)
    {
        if (translationRequest.SubtitleToTranslate != null)
        {
            var existing = await _dbContext.TranslationRequests
                .Where(activeRequest =>
                    activeRequest.SubtitleToTranslate == translationRequest.SubtitleToTranslate &&
                    activeRequest.TargetLanguage == translationRequest.TargetLanguage &&
                    new[]
                        {
                            TranslationStatus.Pending, 
                            TranslationStatus.InProgress
                        }.Contains(activeRequest.Status))
                .Select(activeRequest => new { activeRequest.Id })
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                _logger.LogInformation(
                    "Duplicate translation request skipped for '{Subtitle}' -> '{Language}' (existing id {Id}).",
                    translationRequest.SubtitleToTranslate, translationRequest.TargetLanguage, existing.Id);
                return existing.Id;
            }
        }

        // Create a new TranslationRequest to not keep ID and JobID
        var translationRequestCopy = new TranslationRequest
        {
            MediaId = translationRequest.MediaId,
            Title = translationRequest.Title,
            SourceLanguage = translationRequest.SourceLanguage,
            TargetLanguage = translationRequest.TargetLanguage,
            SubtitleToTranslate = translationRequest.SubtitleToTranslate,
            MediaType = translationRequest.MediaType,
            Status = TranslationStatus.Pending,
            JobType = TranslationJobType.Translation
        };

        _dbContext.TranslationRequests.Add(translationRequestCopy);
        await _dbContext.SaveChangesAsync();
        await _eventService.LogEvent(translationRequestCopy.Id, TranslationStatus.Pending);

        var jobId = _backgroundJobClient.Enqueue<TranslationJob>(job =>
            job.Execute(translationRequestCopy, CancellationToken.None)
        );
        await UpdateTranslationRequest(translationRequestCopy, TranslationStatus.Pending, jobId);

        await UpdateActiveCount();

        return translationRequestCopy.Id;
    }
    
    /// <inheritdoc />
    public async Task CreateBulkRequest(BulkTranslateRequest request)
    {
        var sourceLanguages = await _settingService.GetSettingAsJson<SourceLanguage>(
            SettingKeys.Translation.SourceLanguages
            );
        var sourceCodes = sourceLanguages.Select(sourceLanguage => sourceLanguage.Code).ToHashSet();
        var ignoreCaptions = await _settingService.GetSetting(SettingKeys.Translation.IgnoreCaptions) ?? "false";

        switch (request.MediaType)
        {
            case MediaType.Movie:
                var movies = await _dbContext.Movies
                    .Where(movie => request.MediaIds.Contains(movie.Id))
                    .ToListAsync();

                foreach (var movie in movies)
                {
                    if (movie.Path == null)
                    {
                        _logger.LogInformation("Bulk: skipping movie {Id} — path is null", movie.Id);
                        continue;
                    }
                    await ProcessMediaSubtitles(
                        movie.Path, 
                        movie.FileName, 
                        movie.Id, 
                        MediaType.Movie,
                        request.TargetLanguage, 
                        sourceCodes, 
                        ignoreCaptions);
                }
                break;

            case MediaType.Show:
                var shows = await _dbContext.Shows
                    .Include(show => show.Seasons)
                    .ThenInclude(season => season.Episodes)
                    .Where(show => request.MediaIds.Contains(show.Id))
                    .ToListAsync();

                foreach (var show in shows)
                {
                    foreach (var season in show.Seasons)
                    {
                        if (string.IsNullOrEmpty(season.Path))
                        {
                            continue;
                        }

                        foreach (var episode in season.Episodes)
                        {
                            if (string.IsNullOrEmpty(episode.FileName) || string.IsNullOrEmpty(episode.Path))
                            {
                                continue;
                            }
                            await ProcessMediaSubtitles(
                                season.Path, 
                                episode.FileName, 
                                episode.Id, 
                                MediaType.Episode,
                                request.TargetLanguage,
                                sourceCodes, 
                                ignoreCaptions);
                        }
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Discovers subtitles for a media item, resolves the source subtitle, and creates a translation request.
    /// </summary>
    private async Task ProcessMediaSubtitles(
        string path, 
        string? fileName, 
        int mediaId, 
        MediaType mediaType,
        string targetLanguage, 
        HashSet<string> sourceCodes, 
        string ignoreCaptions)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            _logger.LogDebug("Bulk: skipping mediaId {MediaId} — fileName is empty", mediaId);
            return;
        }

        var subtitles = await _subtitleService.GetSubtitles(path, fileName);
        var selected = _subtitleService.SelectSourceSubtitle(subtitles, sourceCodes, ignoreCaptions);
        if (selected == null)
        {
            _logger.LogDebug("Bulk: skipping mediaId {MediaId} — no source subtitle found (sourceCodes: {SourceCodes}, available: {Available})",
                mediaId, string.Join(", ", sourceCodes),
                string.Join(", ", subtitles.Select(s => s.Language)));
            return;
        }

        if (selected.AvailableLanguages.Contains(targetLanguage.ToLowerInvariant()))
        {
            _logger.LogDebug("Bulk: skipping mediaId {MediaId} — target language {Target} already exists",
                mediaId, targetLanguage);
            return;
        }

        await CreateRequest(new TranslateAbleSubtitle
        {
            MediaId = mediaId,
            MediaType = mediaType,
            SubtitlePath = selected.Subtitle.Path,
            TargetLanguage = targetLanguage,
            SourceLanguage = selected.SourceLanguage,
            SubtitleFormat = selected.Subtitle.Format
        });
    }

    /// <inheritdoc />
    public Task<List<ActiveTranslation>> GetActiveTranslations()
    {
        return _dbContext.TranslationRequests
            .Where(translationRequest =>
                translationRequest.Status == TranslationStatus.Pending ||
                translationRequest.Status == TranslationStatus.InProgress)
            .Select(translationRequest => new ActiveTranslation
            {
                MediaId = translationRequest.MediaId,
                MediaType = translationRequest.MediaType,
                Status = translationRequest.Status
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ActiveTranslation>> UpdateActiveCount()
    {
        var activeTranslations = await GetActiveTranslations();
        await _hubContext.Clients.Group("TranslationRequests")
            .SendAsync("ActiveTranslations", activeTranslations);

        return activeTranslations;
    }
    
    /// <inheritdoc />
    public async Task<string?> CancelTranslationRequest(TranslationRequest cancelRequest)
    {
        var translationRequest = await _dbContext.TranslationRequests.FirstOrDefaultAsync(
            translationRequest => translationRequest.Id == cancelRequest.Id);
        if (translationRequest == null)
        {
            return null;
        }

        if (translationRequest.JobId != null)
        {
            _backgroundJobClient.Delete(translationRequest.JobId);
        }
        else if (_asyncTranslationJobs.TryGetValue(translationRequest.Id, out var cts))
        {
            // Maybe an async translation job
            await cts.CancelAsync();
        }

        if (translationRequest.JobType == TranslationJobType.Proofread &&
            !string.IsNullOrEmpty(translationRequest.TranslatedSubtitle))
        {
            translationRequest.Status = TranslationStatus.Completed;
            translationRequest.ErrorMessage = null;
            await _dbContext.SaveChangesAsync();
            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Cancelled, "Proofread was cancelled");
            await UpdateActiveCount();
            await _progressService.Emit(translationRequest, 100);

            return $"Translation request with id {cancelRequest.Id} has been cancelled";
        }

        translationRequest.CompletedAt = DateTime.UtcNow;
        translationRequest.Status = TranslationStatus.Cancelled;
        translationRequest.ErrorMessage = "Translation was cancelled";
        await _dbContext.SaveChangesAsync();
        await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Cancelled, "Translation was cancelled");
        await UpdateActiveCount();
        await _progressService.Emit(translationRequest, 0);

        return $"Translation request with id {cancelRequest.Id} has been cancelled";
    }
    
    /// <inheritdoc />
    public async Task<string?> RemoveTranslationRequest(TranslationRequest cancelRequest)
    {
        var translationRequest = await _dbContext.TranslationRequests.FirstOrDefaultAsync(
            translationRequest => translationRequest.Id == cancelRequest.Id);
        if (translationRequest == null)
        {
            return null;
        }
        
        _dbContext.TranslationRequests.Remove(translationRequest);
        await _dbContext.SaveChangesAsync();
        
        return $"Translation request with id {cancelRequest.Id} has been removed";
    }

    /// <inheritdoc />
    public async Task<string?> RetryTranslationRequest(TranslationRequest retryRequest)
    {
        var translationRequest = await _dbContext.TranslationRequests.FirstOrDefaultAsync(
            translationRequest => translationRequest.Id == retryRequest.Id);
        if (translationRequest == null)
        {
            return null;
        }


        var newTranslationRequestId = await EnqueueRequest(translationRequest);
        return $"Translation request with id {retryRequest.Id} has been restarted, new job id {newTranslationRequestId}";
    }

    /// <inheritdoc />
    public async Task<string?> ResumeTranslationRequest(TranslationRequest resumeRequest)
    {
        var translationRequest = await _dbContext.TranslationRequests.FirstOrDefaultAsync(
            tr => tr.Id == resumeRequest.Id);
        if (translationRequest == null)
        {
            return null;
        }

        var resumable = new[]
        {
            TranslationStatus.Failed,
            TranslationStatus.Cancelled,
            TranslationStatus.Interrupted
        };
        if (!resumable.Contains(translationRequest.Status))
        {
            _logger.LogInformation(
                "Resume skipped for request {Id}: status {Status} is not resumable.",
                translationRequest.Id, translationRequest.Status);
            return null;
        }

        translationRequest.Status = TranslationStatus.Pending;
        translationRequest.ErrorMessage = null;
        translationRequest.StackTrace = null;
        translationRequest.CompletedAt = null;
        translationRequest.JobType = TranslationJobType.Translation;
        await _dbContext.SaveChangesAsync();
        await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Pending, "Resumed");

        var jobId = _backgroundJobClient.Enqueue<TranslationJob>(job =>
            job.Execute(translationRequest, CancellationToken.None));
        await UpdateTranslationRequest(translationRequest, TranslationStatus.Pending, jobId);
        await UpdateActiveCount();

        return $"Translation request with id {resumeRequest.Id} has been resumed, new job id {jobId}";
    }
    
    /// <inheritdoc />
    public async Task<ProofreadStatusResponse> GetProofreadStatus()
    {
        var entry = await ResolveProofreadService();
        return new ProofreadStatusResponse
        {
            Supported = entry != null,
            Service = entry?.Name
        };
    }

    /// <inheritdoc />
    public async Task<string?> ProofreadLine(
        ProofreadLineRequest proofreadLineRequest,
        CancellationToken cancellationToken)
    {
        var entry = await ResolveProofreadService();
        if (entry == null)
        {
            return null;
        }

        return await entry.Value.Service.ProofreadAsync(
            proofreadLineRequest.SourceLine,
            proofreadLineRequest.TranslatedLine,
            proofreadLineRequest.SourceLanguage,
            proofreadLineRequest.TargetLanguage,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> ProofreadTranslationRequest(TranslationRequest proofreadRequest)
    {
        var translationRequest = await _dbContext.TranslationRequests.FirstOrDefaultAsync(
            candidate => candidate.Id == proofreadRequest.Id);
        if (translationRequest == null)
        {
            return null;
        }

        if (translationRequest.Status != TranslationStatus.Completed)
        {
            _logger.LogInformation(
                "Proofread skipped for request {Id}: status {Status} is not Completed.",
                translationRequest.Id, translationRequest.Status);
            return null;
        }

        if (string.IsNullOrEmpty(translationRequest.TranslatedSubtitle))
        {
            _logger.LogInformation(
                "Proofread skipped for request {Id}: no translated subtitle is recorded.",
                translationRequest.Id);
            return null;
        }

        translationRequest.Status = TranslationStatus.Pending;
        translationRequest.ErrorMessage = null;
        translationRequest.StackTrace = null;
        translationRequest.JobType = TranslationJobType.Proofread;
        await _dbContext.SaveChangesAsync();

        var jobId = _backgroundJobClient.Enqueue<ProofreadJob>(job =>
            job.Execute(translationRequest, CancellationToken.None));
        await UpdateTranslationRequest(translationRequest, TranslationStatus.Pending, jobId);
        await UpdateActiveCount();

        return $"Translation request with id {proofreadRequest.Id} is being proofread, new job id {jobId}";
    }

    /// <inheritdoc />
    public async Task<string?> ApplyProofreadLine(ProofreadLineApplyRequest applyRequest)
    {
        var translationRequest = await _dbContext.TranslationRequests.FirstOrDefaultAsync(
            candidate => candidate.Id == applyRequest.Id);
        if (translationRequest == null || string.IsNullOrEmpty(translationRequest.TranslatedSubtitle))
        {
            return null;
        }

        var stripSubtitleFormatting =
            await _settingService.GetSetting(SettingKeys.Translation.StripSubtitleFormatting) == "true";

        var translatedSubtitles = await _subtitleService.ReadSubtitles(translationRequest.TranslatedSubtitle);
        var target = translatedSubtitles.FirstOrDefault(subtitle => subtitle.Position == applyRequest.Position);
        if (target == null)
        {
            return null;
        }

        var sourceSubtitles = await _subtitleService.ReadSubtitles(translationRequest.SubtitleToTranslate);
        var sourceSubtitle = sourceSubtitles.FirstOrDefault(subtitle => subtitle.Position == applyRequest.Position);
        var sourceText = sourceSubtitle == null
            ? string.Empty
            : string.Join(" ", stripSubtitleFormatting ? sourceSubtitle.PlaintextLines : sourceSubtitle.Lines);

        foreach (var subtitle in translatedSubtitles)
        {
            subtitle.TranslatedLines = subtitle.Lines;
        }
        target.TranslatedLines = [applyRequest.Target];

        var service = applyRequest.Origin == ProofreadLineOrigin.Manual
            ? ManualService
            : (await ResolveProofreadService())?.Name;

        await _progressService.EmitProofreadLine(
            translationRequest,
            applyRequest.Position,
            sourceText,
            applyRequest.Target,
            service);

        await _subtitleService.WriteSubtitles(
            translationRequest.TranslatedSubtitle,
            translatedSubtitles,
            stripSubtitleFormatting);

        return $"Line {applyRequest.Position} of translation request with id {applyRequest.Id} has been updated";
    }

    private async Task<(string Name, IProofreadService Service)?> ResolveProofreadService()
    {
        var serviceNames = TranslationServices.Parse(
            await _settingService.GetSetting(SettingKeys.Translation.ServiceType),
            _logger);
        var services = _translationServiceFactory.CreateTranslationServices(serviceNames);

        foreach (var entry in services)
        {
            if (entry.Service is IProofreadService proofreadService)
            {
                return (entry.Name, proofreadService);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TranslationRequest> UpdateTranslationRequest(TranslationRequest translationRequest,
        TranslationStatus status, string? jobId = null)
    {
        var request = await _dbContext.TranslationRequests.FindAsync(translationRequest.Id);
        if (request == null)
        {
            throw new NotFoundException($"TranslationRequest with ID {translationRequest.Id} not found.");
        }

        if (jobId != null)
        {
            request.JobId = jobId;
        }
        request.Status = status;
        await _dbContext.SaveChangesAsync();

        return request;
    }
    
    /// <inheritdoc />
    public async Task ResumeTranslationRequests()
    {
        var requests = await _dbContext.TranslationRequests
            .Where(tr => tr.Status == TranslationStatus.Pending || 
                         tr.Status == TranslationStatus.InProgress)
            .ToListAsync();

        foreach (var request in requests)
        {
            if (request.JobType == TranslationJobType.Proofread &&
                !string.IsNullOrEmpty(request.TranslatedSubtitle))
            {
                await _eventService.LogEvent(request.Id, TranslationStatus.Interrupted,
                    "Proofread did not finish, translation is unchanged");
                await UpdateTranslationRequest(request, TranslationStatus.Completed);
                continue;
            }

            if (request.JobId == null)
            {
                // Async translation job. Set as Interrupted and don't run
                // Those cannot be resumed
                await UpdateTranslationRequest(request, TranslationStatus.Interrupted);
                await _eventService.LogEvent(request.Id, TranslationStatus.Interrupted);
                continue;
            }

            var jobId = _backgroundJobClient.Enqueue<TranslationJob>(job =>
                job.Execute(request, CancellationToken.None)
            );
            await UpdateTranslationRequest(request, TranslationStatus.Pending, jobId);
        }
    }
    
    /// <inheritdoc />
    public async Task<PagedResult<TranslationRequest>> GetTranslationRequests(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.TranslationRequests
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(translationRequest => translationRequest.Title.ToLower().Contains(searchQuery.ToLower()));
        }
    
        query = orderBy switch
        {
            "Title" => ascending 
                ? query.OrderBy(m => m.Title) 
                : query.OrderByDescending(m => m.Title),
            "CreatedAt" => ascending
                ? query.OrderByDescending(tr => tr.CreatedAt)
                : query.OrderBy(tr => tr.CreatedAt),
            "CompletedAt" => ascending
                ? query.OrderByDescending(tr => tr.CompletedAt)
                : query.OrderBy(tr => tr.CompletedAt),
            _ => ascending
                ? query.OrderByDescending(tr => tr.CreatedAt)
                : query.OrderBy(tr => tr.CreatedAt)
        };
        
        var totalCount = await query.CountAsync();
        var requests = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TranslationRequest>
        {
            Items = requests,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    
    /// <inheritdoc />
    public async Task ClearMediaHash(TranslationRequest translationRequest)
    {
        if (translationRequest.MediaId.HasValue)
        {
            switch (translationRequest.MediaType)
            {
                case MediaType.Movie:
                    var movie = await _dbContext.Movies.FirstOrDefaultAsync(m => m.Id == translationRequest.MediaId.Value);
                    if (movie != null)
                    {
                        movie.MediaHash = string.Empty;
                    }
                    break;
                
                case MediaType.Episode:
                    var episode = await _dbContext.Episodes.FirstOrDefaultAsync(e => e.Id == translationRequest.MediaId.Value);
                    if (episode != null)
                    {
                        episode.MediaHash = string.Empty;
                    }
                    break;
            }
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<BatchTranslatedLine[]> TranslateContentAsync(
        TranslateAbleSubtitleContent translateAbleContent,
        CancellationToken parentCancellationToken)
    {
        // Resolve internal media id from the client's ArrMediaId
        var mediaId = await GetMediaId(translateAbleContent.ArrMediaId, translateAbleContent.MediaType);

        // Derive canonical title from the media lookup (FormatMediaTitle queries the DB for
        // the resolved movie/episode title). The client's Title becomes an optional override.
        var canonicalTitle = await FormatMediaTitle(mediaId, translateAbleContent.MediaType);
        var contentTitle = !string.IsNullOrWhiteSpace(translateAbleContent.Title?.Trim())
            ? translateAbleContent.Title.Trim()
            : canonicalTitle;

        var translationRequest = new TranslationRequest
        {
            MediaId = mediaId,
            Title = contentTitle,
            SourceLanguage = translateAbleContent.SourceLanguage,
            TargetLanguage = translateAbleContent.TargetLanguage,
            SubtitleToTranslate = translateAbleContent.SourceSubtitlePath,
            TranslatedSubtitle = translateAbleContent.TranslatedSubtitlePath,
            MediaType = translateAbleContent.MediaType,
            Status = TranslationStatus.InProgress,
            JobType = TranslationJobType.Translation
        };

        // Link cancel token with new source to be able to cancel the async translation
        var asyncTranslationCancellationTokenSource = new CancellationTokenSource();
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentCancellationToken, asyncTranslationCancellationTokenSource.Token);
        var cancellationToken = cancellationTokenSource.Token;

        try
        {
            BatchTranslatedLine[]? results;
            // Get Translation Settings
            var settings = await _settingService.GetSettings([
                SettingKeys.Translation.UseBatchTranslation,
                SettingKeys.Translation.ServiceType,
                SettingKeys.Translation.MaxBatchSize,
                SettingKeys.Translation.StripSubtitleFormatting,
                SettingKeys.Translation.PreserveLineBreaks
            ]);
            var preserveLineBreaks = settings[SettingKeys.Translation.PreserveLineBreaks] == "true";
            var serviceNames = TranslationServices.Parse(settings[SettingKeys.Translation.ServiceType]);
            var serviceType = serviceNames[0];
            var services = _translationServiceFactory.CreateTranslationServices(serviceNames);
            if (services.Count == 0)
            {
                throw new TranslationException(
                    $"No usable translation services configured: [{string.Join(", ", serviceNames)}]");
            }
            var translationService = services[0].Service;

            // Skip if an active content-translation row for this media+target already exists.
            var existingId = await _dbContext.TranslationRequests
                .Where(r => r.MediaId == translationRequest.MediaId
                         && r.MediaType == translationRequest.MediaType
                         && r.Title == translationRequest.Title
                         && r.SourceLanguage == translationRequest.SourceLanguage
                         && r.TargetLanguage == translationRequest.TargetLanguage
                         && (r.Status == TranslationStatus.Pending || r.Status == TranslationStatus.InProgress))
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingId != null)
            {
                _logger.LogInformation(
                    "Duplicate content-translation request skipped for mediaId {MediaId} ({MediaType}) '{Title}' {Src}->{Tgt} (existing id {Id}).",
                    translationRequest.MediaId, translationRequest.MediaType,
                    translationRequest.Title,
                    translationRequest.SourceLanguage, translationRequest.TargetLanguage, existingId.Value);
                return Array.Empty<BatchTranslatedLine>();
            }

            // Add TranslationRequest
            _dbContext.TranslationRequests.Add(translationRequest);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.InProgress);
            await UpdateActiveCount();

            // Add translation as a async translation request with cancellation source
            _asyncTranslationJobs.TryAdd(translationRequest.Id, cancellationTokenSource);


            // Process Translation
            if (settings[SettingKeys.Translation.UseBatchTranslation] == "true"
                && translateAbleContent.Lines.Count > 1
                && services.Any(e => e.Service is IBatchTranslationService))
            {
                _logger.LogInformation("Processing batch translation request with {lineCount} lines from {sourceLanguage} to {targetLanguage}",
                    translateAbleContent.Lines.Count, translateAbleContent.SourceLanguage, translateAbleContent.TargetLanguage);

                var subtitleTranslator = new SubtitleTranslationService(services, _logger, _progressService);
                var totalSize = translateAbleContent.Lines.Count;
                var stripSubtitleFormatting = settings[SettingKeys.Translation.StripSubtitleFormatting] == "true";
                var maxSize = int.TryParse(settings[SettingKeys.Translation.MaxBatchSize], out var batchSize)
                    ? batchSize
                    : 10000;

                _logger.LogDebug("Batch translation configuration: maxSize={maxSize}, stripFormatting={stripFormatting}, totalLines={totalLines}",
                    maxSize, stripSubtitleFormatting, totalSize);

                var subtitleItems = translateAbleContent.Lines.Select(item => new SubtitleItem
                {
                    Position = item.Position,
                    Lines = new List<string> { item.Line },
                    PlaintextLines = new List<string> { item.Line }
                }).ToList();

                await subtitleTranslator.TranslateSubtitlesBatch(
                    subtitleItems,
                    translationRequest,
                    stripSubtitleFormatting,
                    preserveLineBreaks,
                    maxSize,
                    cancellationToken);

                results = subtitleItems.Select(subtitle => new BatchTranslatedLine
                {
                    Position = subtitle.Position,
                    Line = string.Join(" ", subtitle.TranslatedLines)
                }).ToArray();

                _logger.LogInformation("Batch translation completed successfully. Processed {resultCount} translated lines", results.Length);

                await HandleAsyncTranslationCompletion(translationRequest, serviceType, translationService, results, cancellationToken);
                return results;
            }
            else
            {
                _logger.LogInformation("Using individual line translation for {lineCount} lines from {sourceLanguage} to {targetLanguage}",
                    translateAbleContent.Lines.Count,
                    translateAbleContent.SourceLanguage,
                    translateAbleContent.TargetLanguage);

                var subtitleTranslator = new SubtitleTranslationService(services, _logger);
                var tempResults = new List<BatchTranslatedLine>();

                var iteration = 1;
                var total = translateAbleContent.Lines.Count();
                foreach (var item in translateAbleContent.Lines)
                {
                    var translateLine = new TranslateAbleSubtitleLine
                    {
                        SubtitleLine = item.Line,
                        SourceLanguage = translateAbleContent.SourceLanguage,
                        TargetLanguage = translateAbleContent.TargetLanguage
                    };

                    var translatedText = "";
                    string? serviceUsed = null;
                    LanguagePair? pairUsed = null;
                    if (!string.IsNullOrWhiteSpace(translateLine.SubtitleLine))
                    {
                        var result = await subtitleTranslator.TranslateSubtitleLine(translateLine,
                            cancellationToken);
                        translatedText = result.Translation;
                        serviceUsed = result.Service;
                        pairUsed = result.Pair;
                    }

                    tempResults.Add(new BatchTranslatedLine
                    {
                        Position = item.Position,
                        Line = translatedText
                    });

                    await _progressService.EmitLine(translationRequest, item.Position, item.Line, translatedText, serviceUsed, pairUsed);

                    var progress = (int)Math.Round((double)iteration * 100 / total);
                    await _progressService.Emit(translationRequest, progress);
                    iteration++;
                }

                _logger.LogInformation("Individual line translation completed. Processed {resultCount} lines", tempResults.Count);
                results = tempResults.ToArray();

                await HandleAsyncTranslationCompletion(translationRequest, serviceType, translationService, results, cancellationToken);
                return results;
            }
        }
        catch (TaskCanceledException ex)
        {
            // ExecuteUpdateAsync bypasses change-tracking so a concurrent write cannot abort this save.
            var now = DateTime.UtcNow;
            await _dbContext.TranslationRequests
                .Where(r => r.Id == translationRequest.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, TranslationStatus.Cancelled)
                    .SetProperty(r => r.ErrorMessage, ex.Message)
                    .SetProperty(r => r.CompletedAt, (DateTime?)now)
                    .SetProperty(r => r.UpdatedAt, now));
            translationRequest.CompletedAt = now;
            translationRequest.Status = TranslationStatus.Cancelled;
            translationRequest.ErrorMessage = ex.Message;
            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Cancelled, ex.Message);
            await UpdateActiveCount();
            await _progressService.Emit(translationRequest, 0);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating subtitle content");
            var now = DateTime.UtcNow;
            await _dbContext.TranslationRequests
                .Where(r => r.Id == translationRequest.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, TranslationStatus.Failed)
                    .SetProperty(r => r.ErrorMessage, ex.Message)
                    .SetProperty(r => r.StackTrace, ex.ToString())
                    .SetProperty(r => r.CompletedAt, (DateTime?)now)
                    .SetProperty(r => r.UpdatedAt, now));
            translationRequest.CompletedAt = now;
            translationRequest.Status = TranslationStatus.Failed;
            translationRequest.ErrorMessage = ex.Message;
            translationRequest.StackTrace = ex.ToString();
            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Failed, ex.Message);
            await UpdateActiveCount();
            await _progressService.Emit(translationRequest, 0);
            throw;
        }
        finally
        {
            // Remove async translation from async translation jobs
            _asyncTranslationJobs.TryRemove(translationRequest.Id, out _);
        }
    }

    /// <summary>
    /// Get the Lingarr's media id for the Episode or the Show
    /// </summary>
    private async Task<int> GetMediaId(int arrMediaId, MediaType mediaType)
    {
        switch (mediaType)
        {
            case MediaType.Episode:
                return await _mediaService.GetEpisodeIdOrSyncFromSonarrEpisodeId(arrMediaId);
            case MediaType.Movie:
                return await _mediaService.GetMovieIdOrSyncFromRadarrMovieId(arrMediaId);
            default:
                _logger.LogWarning("Unsupported media type: {MediaType} for translate content async", mediaType);
                return 0;
        }
    }

    /// <summary>
    /// Handles a successful async translation job
    /// </summary>
    private async Task HandleAsyncTranslationCompletion(
        TranslationRequest translationRequest,
        string serviceType,
        ITranslationService translationService,
        BatchTranslatedLine[] results,
        CancellationToken cancellationToken)
    {
        await _statisticsService.UpdateTranslationStatisticsFromLines(translationRequest, serviceType, translationService.ModelName, results);

        var now = DateTime.UtcNow;
        await _dbContext.TranslationRequests
            .Where(r => r.Id == translationRequest.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, TranslationStatus.Completed)
                .SetProperty(r => r.CompletedAt, (DateTime?)now)
                .SetProperty(r => r.UpdatedAt, now), cancellationToken);
        translationRequest.CompletedAt = now;
        translationRequest.Status = TranslationStatus.Completed;
        await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Completed);
        await UpdateActiveCount();
        await _progressService.Emit(translationRequest, 100); // Tells the frontend to update translation request to a finished state
    }

    /// <summary>
    /// Formats the media title based on the media type and ID.
    /// </summary>
    /// <param name="mediaId">The internal media ID (Movie or Episode primary key)</param>
    /// <param name="mediaType">The type of media (Movie or Episode)</param>
    private async Task<string> FormatMediaTitle(int mediaId, MediaType mediaType)
    {
        switch (mediaType)
        {
            case MediaType.Movie:
                var movie = await _dbContext.Movies
                    .FirstOrDefaultAsync(m => m.Id == mediaId);
                return movie?.Title ?? "Unknown Movie";

            case MediaType.Episode:
                var episode = await _dbContext.Episodes
                    .Include(e => e.Season)
                    .ThenInclude(s => s.Show)
                    .FirstOrDefaultAsync(e => e.Id == mediaId);

                if (episode == null)
                    return "Unknown Episode";

                // Format: "Show Title - S01E02 - Episode Title"
                return $"{episode.Season.Show.Title} - " +
                       $"S{episode.Season.SeasonNumber:D2}E{episode.EpisodeNumber:D2} - " +
                       $"{episode.Title}";

            default:
                throw new ArgumentException($"Unsupported media type: {mediaType}");
        }
    }
}