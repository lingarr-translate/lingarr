using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class SubtitleTranslationService
{
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

    public async Task<List<SubtitleItem>> TranslateSubtitles(
        List<SubtitleItem> subtitles,
        TranslateAbleSubtitle translateAbleSubtitle,
        string jobId,
        CancellationToken cancellationToken)
    {
        if (_progressService == null)
        {
            throw new TranslationException("Subtitle translator could not be initialized, progress service is null.");
        }

        int iteration = 0;
        int totalSubtitles = subtitles.Count;

        foreach (var subtitle in subtitles)
        {
            for (var index = 0; index < subtitle.Lines.Count; index++)
            {
                subtitle.Lines[index] = await TranslateSubtitleLine(new TranslateAbleSubtitleLine
                {
                    SubtitleLine = subtitle.Lines[index],
                    SourceLanguage = translateAbleSubtitle.SourceLanguage,
                    TargetLanguage = translateAbleSubtitle.TargetLanguage
                });
            }

            iteration++;
            int progress = (int)Math.Round((double)iteration * 100 / totalSubtitles);
            await _progressService.Emit(jobId, progress, false);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return subtitles;
    }

    public async Task<string> TranslateSubtitleLine(TranslateAbleSubtitleLine translateAbleSubtitle)
    {
        try
        {
            return await _translationService.TranslateAsync(
                translateAbleSubtitle.SubtitleLine,
                translateAbleSubtitle.SourceLanguage,
                translateAbleSubtitle.TargetLanguage);
        }
        catch (TranslationException ex)
        {
            _logger.LogError(ex, "Translation failed for subtitle line: {SubtitleLine}",
                translateAbleSubtitle.SubtitleLine);
            throw;
        }
    }
}