using Lingarr.Core.Entities;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Extensions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class SubtitleTranslationService
{
    private const int MaxLineLength = 42;
    private const int SubtitleLineRetryAttempts = 3;
    private const int SubtitleBatchRetryAttempts = 3;
    private const int SubtitleBatchRetryBaseDelayMilliseconds = 100;
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
    /// <param name="preserveLineBreaks">When true, multi-line subtitles are translated keeping their line breaks intact</param>
    /// <param name="contextBefore">Amount of context before the subtitle line</param>
    /// <param name="contextAfter">Amount of context after the subtitle line</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation.</param>
    public async Task<List<SubtitleItem>> TranslateSubtitles(
        List<SubtitleItem> subtitles,
        TranslationRequest translationRequest,
        bool stripSubtitleFormatting,
        bool preserveLineBreaks,
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

        // Many fansub .ass files stack multiple Dialogue lines at the same
        // timestamp with identical plaintext (shadow, glow, border, main
        // layers). Translate each unique (Start, End, plaintext) once per
        // file and reuse the result for the rest. SRT/VTT files almost never
        // share timestamps, so this is a no-op there.
        var translationCache = new Dictionary<string, string>();

        for (var index = 0; index < totalSubtitles; index++)
        {
            var subtitle = subtitles[index];

            if (cancellationToken.IsCancellationRequested)
            {
                _lastProgression = -1;
                break;
            }

            // subtitle already carries a translation from a prior run.
            if (subtitle.TranslatedLines.Count > 0)
            {
                var existingContentLines = stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines;
                if (!(preserveLineBreaks && existingContentLines.Count > 1))
                {
                    var sourceLine = string.Join(" ", existingContentLines);
                    if (!string.IsNullOrWhiteSpace(sourceLine))
                    {
                        var cacheKey = $"{subtitle.StartTime}|{subtitle.EndTime}|{sourceLine}";
                        translationCache.TryAdd(cacheKey, string.Join(" ", subtitle.TranslatedLines));
                    }
                }

                iteration++;
                await EmitProgress(translationRequest, iteration, totalSubtitles);
                continue;
            }

            var contextLinesBefore = BuildContext(subtitles, index, contextBefore, stripSubtitleFormatting, true);
            var contextLinesAfter = BuildContext(subtitles, index, contextAfter, stripSubtitleFormatting, false);

            var contentLines = stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines;
            var subtitleLines = preserveLineBreaks && contentLines.Count > 1
                ? contentLines
                : [string.Join(" ", contentLines)];

            var translatedLines = new List<string>(subtitleLines.Count);
            foreach (var subtitleLine in subtitleLines)
            {
                if (string.IsNullOrWhiteSpace(subtitleLine))
                {
                    translatedLines.Add(subtitleLine);
                    continue;
                }

                var cacheKey = $"{subtitle.StartTime}|{subtitle.EndTime}|{subtitleLine}";
                if (!translationCache.TryGetValue(cacheKey, out var translated))
                {
                    translated = await TranslateSubtitleLineWithRetryAsync(new TranslateAbleSubtitleLine
                    {
                        SubtitleLine = subtitleLine,
                        SourceLanguage = translationRequest.SourceLanguage,
                        TargetLanguage = translationRequest.TargetLanguage,
                        ContextLinesBefore = contextLinesBefore.Count > 0 ? contextLinesBefore : null,
                        ContextLinesAfter = contextLinesAfter.Count > 0 ? contextLinesAfter : null
                    }, subtitle.Position, cancellationToken);

                    translationCache[cacheKey] = translated;
                }

                translatedLines.Add(translated);
            }

            subtitle.TranslatedLines = translatedLines.Count > 1
                ? translatedLines
                : ToSubtitleLines(translatedLines[0], contentLines.Count, preserveLineBreaks, stripSubtitleFormatting, subtitle.Position);

            var sourceText = string.Join(" ", contentLines);
            var translatedText = string.Join(" ", subtitle.TranslatedLines);
            await _progressService!.EmitLine(translationRequest, subtitle.Position, sourceText, translatedText);

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
    /// Translates a subtitle line with a small retry budget before bubbling the failure up.
    /// </summary>
    private async Task<string> TranslateSubtitleLineWithRetryAsync(
        TranslateAbleSubtitleLine translateAbleSubtitle,
        int position,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= SubtitleLineRetryAttempts; attempt++)
        {
            try
            {
                return await TranslateSubtitleLine(translateAbleSubtitle, cancellationToken);
            }
            catch (TranslationException ex) when (!cancellationToken.IsCancellationRequested && attempt < SubtitleLineRetryAttempts)
            {
                _logger.LogWarning(ex,
                    "Translation failed for subtitle position {Position} on attempt {Attempt}/{MaxAttempts}; retrying.",
                    position,
                    attempt,
                    SubtitleLineRetryAttempts);
            }
        }

        throw new TranslationException($"Translation failed after {SubtitleLineRetryAttempts} attempts for subtitle position {position}.");
    }
    /// <summary>
    /// Translates subtitles in batch mode
    /// </summary>
    /// <param name="subtitles">The list of subtitle items to translate.</param>
    /// <param name="translationRequest">Contains the source and target language specifications.</param>
    /// <param name="stripSubtitleFormatting">Boolean used for indicating that styles need to be stripped from the subtitle</param>
    /// <param name="preserveLineBreaks">When true, multi-line subtitles are translated keeping their line breaks intact</param>
    /// <param name="batchSize">Number of subtitles to process in each batch (0 for all)</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation.</param>
    public async Task<List<SubtitleItem>> TranslateSubtitlesBatch(
        List<SubtitleItem> subtitles,
        TranslationRequest translationRequest,
        bool stripSubtitleFormatting,
        bool preserveLineBreaks,
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

        for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
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

            var newlyTranslated = await ProcessSubtitleBatch(currentBatch,
                batchTranslationService,
                translationRequest.SourceLanguage,
                translationRequest.TargetLanguage,
                stripSubtitleFormatting,
                preserveLineBreaks,
                cancellationToken);

            if (newlyTranslated.Count > 0)
            {
                var lineData = newlyTranslated.Select(s => new TranslatedLineData
                {
                    Position = s.Position,
                    Source = string.Join(" ", stripSubtitleFormatting ? s.PlaintextLines : s.Lines),
                    Target = string.Join(" ", s.TranslatedLines)
                }).ToList();
                await _progressService!.EmitLines(translationRequest, lineData);
            }

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
    /// <param name="preserveLineBreaks">When true, multi-line subtitles are sent and parsed back with their line breaks preserved</param>
    /// <param name="cancellationToken">Token to support cancellation of the translation operation</param>
    /// <returns>The subset of <paramref name="currentBatch"/> that was newly translated by this call (excludes items resumed from a prior run).</returns>
    public async Task<List<SubtitleItem>> ProcessSubtitleBatch(
        List<SubtitleItem> currentBatch,
        IBatchTranslationService batchTranslationService,
        string sourceLanguage,
        string targetLanguage,
        bool stripSubtitleFormatting,
        bool preserveLineBreaks,
        CancellationToken cancellationToken)
    {
        // subtitle already carries a translation from a prior run.
        var toTranslate = currentBatch.Where(s => s.TranslatedLines.Count == 0).ToList();
        if (toTranslate.Count == 0)
        {
            return [];
        }

        var lineSeparator = preserveLineBreaks ? "\n" : " ";
        var batchItems = toTranslate.Select(subtitle =>
        {
            var contentLines = stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines;
            return new BatchSubtitleItem
            {
                Position = subtitle.Position,
                Line = string.Join(lineSeparator, contentLines)
            };
        }).ToList();

        var batchResults = await TranslateBatchWithRetryAsync(
            batchTranslationService,
            batchItems,
            sourceLanguage,
            targetLanguage,
            cancellationToken);

        foreach (var subtitle in toTranslate)
        {
            var contentLines = stripSubtitleFormatting ? subtitle.PlaintextLines : subtitle.Lines;
            if (!batchResults.TryGetValue(subtitle.Position, out var translated))
            {
                _logger.LogWarning("Translation not found for subtitle at position {Position} using original line.", subtitle.Position);
                subtitle.TranslatedLines = contentLines;
                continue;
            }

            if (stripSubtitleFormatting)
            {
                translated = SubtitleFormatterService.RemoveMarkup(translated);
            }

            subtitle.TranslatedLines = ToSubtitleLines(
                translated,
                contentLines.Count,
                preserveLineBreaks,
                stripSubtitleFormatting,
                subtitle.Position);
        }

        return toTranslate;
    }

    /// <summary>
    /// Translates a batch with a small retry budget before bubbling the failure up.
    /// </summary>
    private async Task<Dictionary<int, string>> TranslateBatchWithRetryAsync(
        IBatchTranslationService batchTranslationService,
        List<BatchSubtitleItem> batchItems,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        TranslationException? lastException = null;

        for (var attempt = 1; attempt <= SubtitleBatchRetryAttempts; attempt++)
        {
            try
            {
                return await batchTranslationService.TranslateBatchAsync(
                    batchItems,
                    sourceLanguage,
                    targetLanguage,
                    cancellationToken);
            }
            catch (TranslationException ex) when (!cancellationToken.IsCancellationRequested && attempt < SubtitleBatchRetryAttempts)
            {
                lastException = ex;
                var delayMilliseconds = SubtitleBatchRetryBaseDelayMilliseconds * (1 << (attempt - 1));
                _logger.LogWarning(ex,
                    "Batch translation failed for {Count} subtitles on attempt {Attempt}/{MaxAttempts}; retrying in {Delay}ms.",
                    batchItems.Count,
                    attempt,
                    SubtitleBatchRetryAttempts,
                    delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
            }
            catch (TranslationException ex)
            {
                lastException = ex;
            }
        }

        throw new TranslationException(
            $"Batch translation failed after {SubtitleBatchRetryAttempts} attempts.",
            lastException);
    }

    /// <summary>
    /// Resolves a raw translated string back into a list of subtitle lines.
    /// When line breaks are being preserved, splits on '\n' and validates the count matches the input.
    /// When the input was merged, optionally rewraps the output to <see cref="MaxLineLength"/> if formatting is stripped.
    /// </summary>
    private List<string> ToSubtitleLines(
        string translated,
        int originalLineCount,
        bool preserveLineBreaks,
        bool stripSubtitleFormatting,
        int position)
    {
        if (preserveLineBreaks && originalLineCount > 1)
        {
            var splitLines = translated.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            if (splitLines.Count == originalLineCount)
            {
                return splitLines;
            }

            _logger.LogWarning(
                "Line break count mismatch at position {Position} (expected {Expected}, got {Actual}); collapsing to single line.",
                position, originalLineCount, splitLines.Count);
            return [string.Join(" ", splitLines)];
        }

        return originalLineCount > 1 && stripSubtitleFormatting
            ? translated.SplitIntoLines(MaxLineLength)
            : [translated];
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
        var progress = (int)Math.Round((double)iteration * 100 / total);

        if (progress != _lastProgression)
        {
            _logger.LogInformation($"Progress: {progress}% (Subtitle {iteration} of {total})");
            await _progressService!.Emit(request, progress);
            _lastProgression = progress;
        }
    }
}
