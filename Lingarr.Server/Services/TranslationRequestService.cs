using DeepL;
using Hangfire;
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
            .Where(l => l.TranslationRequestId == id)
            .OrderBy(l => l.Position)
            .Select(l => new TranslationRequestSubtitleLines
            {
                Position = l.Position,
                Source = l.Source,
                Target = l.Target
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
        var mediaTitle = await FormatMediaTitle(translateAbleSubtitle);
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
            Status = TranslationStatus.Pending
        };

        _dbContext.TranslationRequests.Add(translationRequestCopy);
        await _dbContext.SaveChangesAsync();
        await _eventService.LogEvent(translationRequestCopy.Id, TranslationStatus.Pending);

        var jobId = _backgroundJobClient.Enqueue<TranslationJob>(job =>
            job.Execute(translationRequestCopy, CancellationToken.None)
        );
        await UpdateTranslationRequest(translationRequestCopy, TranslationStatus.Pending, jobId);

        var count = await GetActiveCount();
        await _hubContext.Clients.Group("TranslationRequests").SendAsync("RequestActive", new
        {
            count
        });

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
    public async Task<int> GetActiveCount()
    {
        return await _dbContext.TranslationRequests.CountAsync(translation =>
            translation.Status != TranslationStatus.Cancelled &&
            translation.Status != TranslationStatus.Failed &&
            translation.Status != TranslationStatus.Completed &&
            translation.Status != TranslationStatus.Interrupted);

    }

    /// <inheritdoc />
    public async Task<int> UpdateActiveCount()
    {
        var count = await GetActiveCount();
        await _hubContext.Clients.Group("TranslationRequests").SendAsync("RequestActive", new
        {
            count
        });
        
        return count;
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
        // Prepare TranslationRequest Object
        var translationRequest = new TranslationRequest
        {
            MediaId = await GetMediaId(translateAbleContent.ArrMediaId, translateAbleContent.MediaType),
            Title = translateAbleContent.Title,
            SourceLanguage = translateAbleContent.SourceLanguage,
            TargetLanguage = translateAbleContent.TargetLanguage,
            MediaType = translateAbleContent.MediaType,
            Status = TranslationStatus.InProgress
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
                SettingKeys.Translation.StripSubtitleFormatting
            ]);
            var serviceType = settings[SettingKeys.Translation.ServiceType];
            var translationService = _translationServiceFactory.CreateTranslationService(
                serviceType
            );

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
                && translationService is IBatchTranslationService batchService)
            {
                _logger.LogInformation("Processing batch translation request with {lineCount} lines from {sourceLanguage} to {targetLanguage}",
                    translateAbleContent.Lines.Count, translateAbleContent.SourceLanguage, translateAbleContent.TargetLanguage);

                var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);
                var totalSize = translateAbleContent.Lines.Count;
                var maxBatchSize = settings[SettingKeys.Translation.MaxBatchSize];
                var stripSubtitleFormatting = settings[SettingKeys.Translation.StripSubtitleFormatting] == "true";
                var maxSize = int.TryParse(maxBatchSize,
                    out var batchSize)
                    ? batchSize
                    : 10000;

                _logger.LogDebug("Batch translation configuration: maxSize={maxSize}, stripFormatting={stripFormatting}, totalLines={totalLines}",
                    maxSize, stripSubtitleFormatting, totalSize);

                if (maxSize != 0 && totalSize > maxSize)
                {
                    _logger.LogWarning(
                        "Batch size ({Size}) exceeds configured maximum ({Max}). Processing in smaller batches.",
                        totalSize, maxSize);
                    results = await ChunkLargeBatch(
                        translateAbleContent,
                        translationService,
                        batchService,
                        translationRequest,
                        maxSize,
                        stripSubtitleFormatting,
                        cancellationToken);

                    // Handle completion now since we early exit here
                    await HandleAsyncTranslationCompletion(translationRequest, serviceType, translationService, results, cancellationToken);
                    return results; 
                }

                _logger.LogInformation("Processing batch translation within size limits. Converting {lineCount} lines to subtitle items",
                    translateAbleContent.Lines.Count);

                // Convert translateAbleContent items to SubtitleItems for ProcessSubtitleBatch
                var subtitleItems = translateAbleContent.Lines.Select(item => new SubtitleItem
                {
                    Position = item.Position,
                    Lines = new List<string> { item.Line },
                    PlaintextLines = new List<string> { item.Line }
                }).ToList();

                _logger.LogDebug("Starting batch subtitle processing with {itemCount} subtitle items", subtitleItems.Count);

                await subtitleTranslator.ProcessSubtitleBatch(
                    subtitleItems,
                    batchService,
                    translateAbleContent.SourceLanguage,
                    translateAbleContent.TargetLanguage,
                    stripSubtitleFormatting,
                    cancellationToken);

                var batchLineData = subtitleItems.Select(s => new TranslatedLineData
                {
                    Position = s.Position,
                    Source = string.Join(" ", stripSubtitleFormatting ? s.PlaintextLines : s.Lines),
                    Target = string.Join(" ", s.TranslatedLines ?? s.Lines)
                }).ToList();
                await _progressService.EmitLines(translationRequest, batchLineData);

                results = subtitleItems.Select(subtitle => new BatchTranslatedLine
                {
                    Position = subtitle.Position,
                    Line = string.Join(" ", subtitle.TranslatedLines)
                }).ToArray();

                _logger.LogInformation("Batch translation completed successfully. Processed {resultCount} translated lines", results.Length);
            }
            else
            {
                _logger.LogInformation("Using individual line translation for {lineCount} lines from {sourceLanguage} to {targetLanguage}",
                    translateAbleContent.Lines.Count,
                    translateAbleContent.SourceLanguage,
                    translateAbleContent.TargetLanguage);

                var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);
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
                    if (!string.IsNullOrWhiteSpace(translateLine.SubtitleLine))
                    {
                        translatedText = await subtitleTranslator.TranslateSubtitleLine(
                            translateLine,
                            cancellationToken);
                    }

                    tempResults.Add(new BatchTranslatedLine
                    {
                        Position = item.Position,
                        Line = translatedText
                    });

                    await _progressService.EmitLine(translationRequest, item.Position, item.Line, translatedText);

                    var progress = (int)Math.Round((double)iteration * 100 / total);
                    await _progressService.Emit(translationRequest, progress);
                    iteration++;
                }

                _logger.LogInformation("Individual line translation completed. Processed {resultCount} lines", tempResults.Count);
                results = tempResults.ToArray();
            }

            await HandleAsyncTranslationCompletion(translationRequest, serviceType, translationService, results, cancellationToken);
            return results;
        }
        catch (TaskCanceledException ex)
        {
            translationRequest.CompletedAt = DateTime.UtcNow;
            translationRequest.Status = TranslationStatus.Cancelled;
            translationRequest.ErrorMessage = ex.Message;
            await _dbContext.SaveChangesAsync();
            await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Cancelled, ex.Message);
            await UpdateActiveCount();
            await _progressService.Emit(translationRequest, 0);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating subtitle content");
            translationRequest.CompletedAt = DateTime.UtcNow;
            translationRequest.Status = TranslationStatus.Failed;
            translationRequest.ErrorMessage = ex.Message;
            translationRequest.StackTrace = ex.ToString();
            await _dbContext.SaveChangesAsync();
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

        translationRequest.CompletedAt = DateTime.UtcNow;
        translationRequest.Status = TranslationStatus.Completed;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _eventService.LogEvent(translationRequest.Id, TranslationStatus.Completed);
        await UpdateActiveCount();
        await _progressService.Emit(translationRequest, 100); // Tells the frontend to update translation request to a finished state
    }

    /// <summary>
    /// Processes a large batch by breaking it into smaller batches
    /// </summary>
    private async Task<BatchTranslatedLine[]> ChunkLargeBatch(
        TranslateAbleSubtitleContent translateAbleSubtitleContent,
        ITranslationService translationService,
        IBatchTranslationService batchService,
        TranslationRequest translationRequest,
        int maxBatchSize,
        bool stripSubtitleFormatting,
        CancellationToken cancellationToken)
    {
        var results = new List<BatchTranslatedLine>();
        var currentBatch = new List<SubtitleItem>();
        var subtitleTranslator = new SubtitleTranslationService(translationService, _logger);

        var totalLines = translateAbleSubtitleContent.Lines.Count;
        var totalBatches = (int)Math.Ceiling((double)totalLines / maxBatchSize);
        var processedBatches = 1;

        foreach (var item in translateAbleSubtitleContent.Lines)
        {
            if (currentBatch.Count >= maxBatchSize)
            {
                await ProcessBatch(currentBatch, subtitleTranslator, batchService,
                    translationRequest,
                    translateAbleSubtitleContent.SourceLanguage, translateAbleSubtitleContent.TargetLanguage,
                    stripSubtitleFormatting, results, cancellationToken);
                currentBatch.Clear();

                // Report progress
                processedBatches++;
                var progress = (int)Math.Round((double)processedBatches * 100 / totalBatches);
                await _progressService.Emit(translationRequest, progress);
            }

            currentBatch.Add(new SubtitleItem
            {
                Position = item.Position,
                Lines =
                [
                    item.Line
                ],
                PlaintextLines =
                [
                    item.Line
                ]
            });
        }

        if (currentBatch.Count > 0)
        {
            await ProcessBatch(currentBatch, subtitleTranslator, batchService,
                translationRequest,
                translateAbleSubtitleContent.SourceLanguage, translateAbleSubtitleContent.TargetLanguage,
                stripSubtitleFormatting, results, cancellationToken);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Processes a single batch and adds results to the results collection
    /// </summary>
    private async Task ProcessBatch(
        List<SubtitleItem> batch,
        SubtitleTranslationService subtitleTranslator,
        IBatchTranslationService batchService,
        TranslationRequest translationRequest,
        string sourceLanguage,
        string targetLanguage,
        bool stripSubtitleFormatting,
        List<BatchTranslatedLine> results,
        CancellationToken cancellationToken)
    {
        await subtitleTranslator.ProcessSubtitleBatch(
            batch,
            batchService,
            sourceLanguage,
            targetLanguage,
            stripSubtitleFormatting,
            cancellationToken);

        var lineData = batch.Select(s => new TranslatedLineData
        {
            Position = s.Position,
            Source = string.Join(" ", stripSubtitleFormatting ? s.PlaintextLines : s.Lines),
            Target = string.Join(" ", s.TranslatedLines ?? s.Lines)
        }).ToList();
        await _progressService.EmitLines(translationRequest, lineData);

        results.AddRange(batch.Select(subtitle => new BatchTranslatedLine
        {
            Position = subtitle.Position,
            Line = string.Join(" ", subtitle.TranslatedLines ?? subtitle.Lines)
        }));
    }
    
    /// <summary>
    /// Formats the media title based on the media type and ID.
    /// </summary>
    /// <param name="translateAbleSubtitle">The subtitle information containing media type and ID</param>
    private async Task<string> FormatMediaTitle(TranslateAbleSubtitle translateAbleSubtitle)
    {
        switch (translateAbleSubtitle.MediaType)
        {
            case MediaType.Movie:
                var movie = await _dbContext.Movies
                    .FirstOrDefaultAsync(m => m.Id == translateAbleSubtitle.MediaId);
                return movie?.Title ?? "Unknown Movie";

            case MediaType.Episode:
                var episode = await _dbContext.Episodes
                    .Include(e => e.Season)
                    .ThenInclude(s => s.Show)
                    .FirstOrDefaultAsync(e => e.Id == translateAbleSubtitle.MediaId);

                if (episode == null)
                    return "Unknown Episode";

                // Format: "Show Title - S01E02 - Episode Title"
                return $"{episode.Season.Show.Title} - " +
                       $"S{episode.Season.SeasonNumber:D2}E{episode.EpisodeNumber:D2} - " +
                       $"{episode.Title}";

            default:
                throw new ArgumentException($"Unsupported media type: {translateAbleSubtitle.MediaType}");
        }
    }
}