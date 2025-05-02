using Lingarr.Core.Entities;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;

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
            var translated = await TranslateSubtitleLine(new TranslateAbleSubtitleLine
                {
                    SubtitleLine = subtitleLine,
                    SourceLanguage = translationRequest.SourceLanguage,
                    TargetLanguage = translationRequest.TargetLanguage,
                    ContextLinesBefore = contextLinesBefore.Count > 0 ? contextLinesBefore : null,
                    ContextLinesAfter = contextLinesAfter.Count > 0 ? contextLinesAfter : null
                },
                cancellationToken);

            // Rebuild lines based on max length
            subtitle.TranslatedLines = translated.Split(' ')
                // Start with a list containing one empty string - this will be our first line
                .Aggregate(new List<string> { "" },

                    // For each word, we look at our lines and the current word
                    (lines, word) =>
                    {
                        // Get the last line we're currently building
                        var currentLine = lines.Last();

                        // Check if adding this word (plus a space if needed) would exceed MaxLineLength
                        // We only add a space if the current line isn't empty
                        if (currentLine.Length + word.Length + 1 <= MaxLineLength)
                        {
                            // If it fits, append it to the current line
                            // If the line is empty, just use the word
                            // If the line has content, add a space before the word
                            lines[^1] = currentLine.Length == 0 ? word : $"{currentLine} {word}";
                        }
                        else
                        {
                            // If it doesn't fit, start a new line with this word
                            lines.Add(word);
                        }

                        // Return the updated list of lines
                        return lines;
                    });

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