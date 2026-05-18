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
    private int _lastProgression = -1;
    private readonly IReadOnlyList<TranslationServiceEntry> _services;
    private readonly IProgressService? _progressService;
    private readonly ILogger _logger;
    private readonly Dictionary<int, string> _serviceByPosition = [];

    public SubtitleTranslationService(
        IReadOnlyList<TranslationServiceEntry> services,
        ILogger logger,
        IProgressService? progressService = null)
    {
        if (services.Count == 0)
        {
            throw new TranslationException("Subtitle translator could not be initialized, translation services list is empty.");
        }
        _services = services;
        _progressService = progressService;
        _logger = logger;
    }

    /// <summary>
    /// Service used to produce each translated line in the most recent run, keyed by subtitle position.
    /// </summary>
    public IReadOnlyDictionary<int, string> ServiceByPosition => _serviceByPosition;

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
            string? serviceUsed = null;
            foreach (var subtitleLine in subtitleLines)
            {
                if (string.IsNullOrWhiteSpace(subtitleLine))
                {
                    translatedLines.Add(subtitleLine);
                    continue;
                }

                var cacheKey = $"{subtitle.StartTime}|{subtitle.EndTime}|{subtitleLine}";
                if (translationCache.TryGetValue(cacheKey, out var cachedTranslation))
                {
                    translatedLines.Add(cachedTranslation);
                    continue;
                }

                var (translation, service) = await TranslateSubtitleLine(new TranslateAbleSubtitleLine
                {
                    SubtitleLine = subtitleLine,
                    SourceLanguage = translationRequest.SourceLanguage,
                    TargetLanguage = translationRequest.TargetLanguage,
                    ContextLinesBefore = contextLinesBefore.Count > 0 ? contextLinesBefore : null,
                    ContextLinesAfter = contextLinesAfter.Count > 0 ? contextLinesAfter : null
                }, cancellationToken);
                translationCache[cacheKey] = translation;
                translatedLines.Add(translation);
                serviceUsed ??= service;
            }

            subtitle.TranslatedLines = translatedLines.Count > 1
                ? translatedLines
                : ToSubtitleLines(translatedLines[0], contentLines.Count, preserveLineBreaks, stripSubtitleFormatting, subtitle.Position);

            var sourceText = string.Join(" ", contentLines);
            var translatedText = string.Join(" ", subtitle.TranslatedLines);
            if (serviceUsed != null)
            {
                _serviceByPosition[subtitle.Position] = serviceUsed;
            }
            await _progressService!.EmitLine(translationRequest, subtitle.Position, sourceText, translatedText, serviceUsed);

            iteration++;
            await EmitProgress(translationRequest, iteration, totalSubtitles);
        }

        _lastProgression = -1;
        return subtitles;
    }

    /// <summary>
    /// Translates a single subtitle line, walking the fallback services on <see cref="TranslationException"/>.
    /// Cancellation propagates without triggering fallback.
    /// </summary>
    /// <returns>The translated line plus the service name that produced it.</returns>
    public async Task<(string Translation, string Service)> TranslateSubtitleLine(
        TranslateAbleSubtitleLine translateAbleSubtitle,
        CancellationToken cancellationToken)
    {
        TranslationException? lastError = null;
        foreach (var entry in _services)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var translated = await entry.Service.TranslateAsync(
                    translateAbleSubtitle.SubtitleLine,
                    translateAbleSubtitle.SourceLanguage,
                    translateAbleSubtitle.TargetLanguage,
                    translateAbleSubtitle.ContextLinesBefore,
                    translateAbleSubtitle.ContextLinesAfter,
                    cancellationToken);
                return (translated, entry.Name);
            }
            catch (TranslationException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Translation service {Service} failed.", entry.Name);
            }
        }
        throw new TranslationException("All configured translation services failed for subtitle line.", lastError);
    }

    /// <summary>
    /// Translates subtitles in batch mode.
    /// </summary>
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

            var newlyTranslated = await ProcessSubtitleBatch(
                currentBatch,
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
                    Target = string.Join(" ", s.TranslatedLines),
                    Service = _serviceByPosition.GetValueOrDefault(s.Position)
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
    /// Translates a batch of subtitles using the first batch-capable provider, falling back to subsequent
    /// batch-capable providers on <see cref="TranslationException"/>, then to per-line translation (which
    /// has its own fallback loop) if no batch provider succeeds.
    /// </summary>
    public async Task<List<SubtitleItem>> ProcessSubtitleBatch(
        List<SubtitleItem> currentBatch,
        string sourceLanguage,
        string targetLanguage,
        bool stripSubtitleFormatting,
        bool preserveLineBreaks,
        CancellationToken cancellationToken)
    {
        var toTranslate = currentBatch.Where(subtitle => subtitle.TranslatedLines.Count == 0).ToList();
        if (toTranslate.Count == 0)
        {
            return [];
        }

        TranslationException? lastError = null;
        foreach (var entry in _services)
        {
            if (entry.BatchService is null)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await RunBatch(
                    entry.BatchService,
                    entry.Name,
                    toTranslate,
                    sourceLanguage,
                    targetLanguage,
                    stripSubtitleFormatting,
                    preserveLineBreaks,
                    cancellationToken);
                return toTranslate;
            }
            catch (TranslationException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Batch translation service {Service} failed.", entry.Name);
            }
        }
        throw new TranslationException("All configured batch translation services failed.", lastError);
    }

    private async Task RunBatch(
        IBatchTranslationService batchService,
        string serviceName,
        List<SubtitleItem> toTranslate,
        string sourceLanguage,
        string targetLanguage,
        bool stripSubtitleFormatting,
        bool preserveLineBreaks,
        CancellationToken cancellationToken)
    {
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

        var batchResults = await batchService.TranslateBatchAsync(
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
            _serviceByPosition[subtitle.Position] = serviceName;
        }
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
