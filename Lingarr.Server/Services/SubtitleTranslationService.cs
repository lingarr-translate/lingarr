using Lingarr.Core.Entities;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Extensions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class SubtitleTranslationService
{
    private const int MaxLineLength = 42;
    private int _lastProgression = -1;
    private readonly ITranslationService _translationService;
    private readonly IProgressService? _progressService;
    private readonly ILogger _logger;

    public SubtitleTranslationService(
        ITranslationService translationService,
        ILogger logger,
        IProgressService? progressService = null)
    {
        _translationService = translationService;
        _progressService = progressService;
        _logger = logger;
    }

    /// <summary>
    /// Translates a list of subtitle items from the source language to the target language.
    /// </summary>
    /// <param name="subtitles">The list of subtitle items to translate.</param>
    /// <param name="translationRequest">Contains the source and target language specifications.</param>
    /// <param name="stripSubtitleFormatting">Boolean used for indicating that styles need to be stripped from the subtitle</param>
    /// <param name="contextBefore">Amount of context before the subtitle line</param>
    /// <param name="contextAfter">Amount of context after the subtitle line</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation.</param>
    public async Task<List<SubtitleItem>> TranslateSubtitles(
        List<SubtitleItem> subtitles,
        TranslationRequest translationRequest,
        bool stripSubtitleFormatting,
        int contextBefore,
        int contextAfter,
        CancellationToken cancellationToken)
    {
        if (_progressService == null)
        {
            throw new TranslationException("Subtitle translator could not be initialized, progress service is null.");
        }

        var iteration = 0;
        var totalSubtitles = subtitles.Count;

        for (var index = 0; index < subtitles.Count; index++)
        {
            var subtitle = subtitles[index];

            if (cancellationToken.IsCancellationRequested)
            {
                _lastProgression = -1;
                break;
            }

            var contextLinesBefore = BuildContext(subtitles, index, contextBefore, stripSubtitleFormatting, true);
            var contextLinesAfter = BuildContext(subtitles, index, contextAfter, stripSubtitleFormatting, false);

            var subtitleLine = string.Join(" ", stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines);
            var translated = "";
            if (subtitleLine != "")
            {
                translated = await TranslateSubtitleLine(new TranslateAbleSubtitleLine
                    {
                        SubtitleLine = subtitleLine,
                        SourceLanguage = translationRequest.SourceLanguage,
                        TargetLanguage = translationRequest.TargetLanguage,
                        ContextLinesBefore = contextLinesBefore.Count > 0 ? contextLinesBefore : null,
                        ContextLinesAfter = contextLinesAfter.Count > 0 ? contextLinesAfter : null
                    },
                    cancellationToken);
            }
            // Rebuild lines based on max length
            subtitle.TranslatedLines = translated.SplitIntoLines(MaxLineLength);

            iteration++;
            await EmitProgress(translationRequest, iteration, totalSubtitles);
        }

        _lastProgression = -1;
        return subtitles;
    }

    /// <summary>
    /// Translates a single subtitle line using the configured translation service.
    /// </summary>
    /// <param name="translateAbleSubtitle">
    /// Contains the subtitle line to translate along with source and target language specifications.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the translation operation</param>
    /// <returns>The translated subtitle line.</returns>
    public async Task<string> TranslateSubtitleLine(
        TranslateAbleSubtitleLine translateAbleSubtitle,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _translationService.TranslateAsync(
                translateAbleSubtitle.SubtitleLine,
                translateAbleSubtitle.SourceLanguage,
                translateAbleSubtitle.TargetLanguage,
                translateAbleSubtitle.ContextLinesBefore,
                translateAbleSubtitle.ContextLinesAfter,
                cancellationToken);
        }
        catch (TranslationException ex)
        {
            _logger.LogError(ex,
                "Translation failed for subtitle line: {SubtitleLine} from {SourceLang} to {TargetLang}",
                translateAbleSubtitle.SubtitleLine,
                translateAbleSubtitle.SourceLanguage,
                translateAbleSubtitle.TargetLanguage);
            throw new TranslationException("Translation failed for subtitle line", ex);
        }
    }
    
    /// <summary>
    /// Translates subtitles in batch mode 
    /// </summary>
    /// <param name="subtitles">The list of subtitle items to translate.</param>
    /// <param name="translationRequest">Contains the source and target language specifications.</param>
    /// <param name="stripSubtitleFormatting">Boolean used for indicating that styles need to be stripped from the subtitle</param>
    /// <param name="batchSize">Number of subtitles to process in each batch (0 for all)</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation.</param>
    public async Task<List<SubtitleItem>> TranslateSubtitlesBatch(
        List<SubtitleItem> subtitles,
        TranslationRequest translationRequest,
        bool stripSubtitleFormatting,
        int batchSize = 0,
        CancellationToken cancellationToken = default)
    {
        if (_progressService == null)
        {
            throw new TranslationException("Subtitle translator could not be initialized, progress service is null.");
        }

        if (_translationService is not IBatchTranslationService batchTranslationService)
        {
            throw new TranslationException("The configured translation service does not support batch translation.");
        }
        
        // If batchSize is 0 or negative, we'll translate all subtitles at once
        if (batchSize <= 0)
        {
            batchSize = subtitles.Count;
        }

        var totalBatches = (int)Math.Ceiling((double)subtitles.Count / batchSize);
        var processedSubtitles = 0;

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _lastProgression = -1;
                break;
            }

            var currentBatch = subtitles
                .Skip(batchIndex * batchSize)
                .Take(batchSize)
                .ToList();
            
            await ProcessSubtitleBatch(currentBatch,
                batchTranslationService,
                translationRequest.SourceLanguage,
                translationRequest.TargetLanguage,
                stripSubtitleFormatting,
                cancellationToken);

            processedSubtitles += currentBatch.Count;
            await EmitProgress(translationRequest, processedSubtitles, subtitles.Count);
        }

        _lastProgression = -1;
        return subtitles;
    }

    /// <summary>
    /// Processes a batch of subtitles by translating them and updating their TranslatedLines property.
    /// </summary>
    /// <param name="currentBatch">The batch of subtitles to process</param>
    /// <param name="batchTranslationService">The batch translation service to use</param>
    /// <param name="sourceLanguage"></param>
    /// <param name="targetLanguage"></param>
    /// <param name="stripSubtitleFormatting">Boolean used for indicating that styles need to be stripped from the subtitle</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation</param>
    public async Task ProcessSubtitleBatch(
        List<SubtitleItem> currentBatch,
        IBatchTranslationService batchTranslationService,
        string sourceLanguage,
        string targetLanguage,
        bool stripSubtitleFormatting,
        CancellationToken cancellationToken)
    {
        var batchItems = currentBatch.Select(subtitle => new BatchSubtitleItem
        {
            Position = subtitle.Position,
            Line = string.Join(" ", stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines)
        }).ToList();

        var batchResults = await batchTranslationService.TranslateBatchAsync(
            batchItems,
            sourceLanguage,
            targetLanguage,
            cancellationToken);
        
        foreach (var subtitle in currentBatch)
        {
            if (batchResults.TryGetValue(subtitle.Position, out var translated))
            {
                if (stripSubtitleFormatting)
                {
                    translated = SubtitleFormatterService.RemoveMarkup(translated);
                }

                // Rebuild lines based on max length
                subtitle.TranslatedLines = translated.SplitIntoLines(MaxLineLength);
            }
            else
            {
                _logger.LogWarning("Translation not found for subtitle at position {Position} using original line.", subtitle.Position);
                subtitle.TranslatedLines = stripSubtitleFormatting ? 
                    subtitle.PlaintextLines : 
                    subtitle.Lines;
            }
        }
    }
    
    /// <summary>
    /// Builds a list of subtitle text strings as context around a given subtitle index.
    /// </summary>
    /// <param name="subtitles">The list of subtitle items.</param>
    /// <param name="startIndex">The index around which to build context.</param>
    /// <param name="count">The number of subtitles to include before or after the index.</param>
    /// <param name="stripSubtitleFormatting">Whether to strip formatting from subtitles.</param>
    /// <param name="isBeforeContext">If true, builds context before the index; otherwise, builds after.</param>
    private static List<string> BuildContext(List<SubtitleItem> subtitles, int startIndex, int count,
        bool stripSubtitleFormatting, bool isBeforeContext)
    {
        List<string> context = [];

        var start = isBeforeContext
            ? Math.Max(0, startIndex - count)
            : startIndex + 1;

        var end = isBeforeContext
            ? startIndex
            : Math.Min(subtitles.Count, startIndex + 1 + count);

        for (var i = start; i < end; i++)
        {
            var contextSubtitle = subtitles[i];
            context.Add(string.Join(" ",
                stripSubtitleFormatting ? contextSubtitle.PlaintextLines : contextSubtitle.Lines));
        }

        return context.Count > 0 ? context : [];
    }

    /// <summary>
    /// Emits translation progress updates if progress has changed since the last emission.
    /// </summary>
    /// <param name="request">The translation request being processed.</param>
    /// <param name="iteration">The current subtitle index being processed.</param>
    /// <param name="total">The total number of subtitles in the request.</param>
    private async Task EmitProgress(TranslationRequest request, int iteration, int total)
    {
        int progress = (int)Math.Round((double)iteration * 100 / total);

        if (progress != _lastProgression)
        {
            _logger.LogDebug($"Progress: {progress}% (Subtitle {iteration} of {total})");
            await _progressService!.Emit(request, progress);
            _lastProgression = progress;
        }
    }
}