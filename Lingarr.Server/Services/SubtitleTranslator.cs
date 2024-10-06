using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class SubtitleTranslator
{
    private readonly ITranslationService _translationService;
    private readonly IProgressService _progressService;
    private readonly ILogger _logger;
    private readonly SubRipParser _subtitleParser;

    public SubtitleTranslator(
        ITranslationService translationService, 
        IProgressService progressService, 
        ILogger logger)
    {
        _translationService = translationService;
        _progressService = progressService;
        _logger = logger;
        _subtitleParser = new SubRipParser();
    }

    public async Task TranslateSubtitlesAsync(
        TranslateAbleSubtitle translateAbleSubtitle,
        string jobId, 
        CancellationToken cancellationToken)
    {
        List<SubtitleItem> subtitles;
        await using (var fileStream = File.OpenRead(translateAbleSubtitle.SubtitlePath))
        {
            subtitles = _subtitleParser.ParseStream(fileStream, Encoding.UTF8);
        }
        
        int iteration = 0;
        int totalSubtitles = subtitles.Count;

        foreach (var subtitle in subtitles)
        {
            for (var index = 0; index < subtitle.Lines.Count; index++)
            {
                try
                {
                    subtitle.Lines[index] = await _translationService.TranslateAsync(
                        subtitle.Lines[index], 
                        translateAbleSubtitle.SourceLanguage, 
                        translateAbleSubtitle.TargetLanguage);
                }
                catch (TranslationException ex)
                {
                    _logger.LogError(ex, "Translation failed for subtitle line {index}", index);
                    throw;
                }
            }

            iteration++;
            int progress = (int)Math.Round((double)iteration * 100 / totalSubtitles);
            await _progressService.Emit(jobId, progress, false);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Translation cancelled for subtitle: {subtitlePath}", translateAbleSubtitle.SubtitlePath);
                return;
            }
        }

        const string pattern = @"(\.([a-zA-Z]{2,3}))?\.srt$";
        var replacement = $".{translateAbleSubtitle.TargetLanguage}.srt";
        var filePath = Regex.Replace(translateAbleSubtitle.SubtitlePath, pattern, replacement);

        var writer = new SubRipWriter();
        await using (var fileStream = File.OpenWrite(filePath))
        {
            await writer.WriteStreamAsync(fileStream, subtitles);
        }
        _logger.LogInformation("TranslateJob completed and created subtitle: {filePath}", filePath);
        await _progressService.Emit(jobId, 100, true);
    }
}