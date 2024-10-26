using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class SubtitleService : ISubtitleService
{
    /// <inheritdoc />
    public async Task<List<Subtitles>> GetAllSubtitles(string path)
    {
        var files = await Task.Run(() => Directory.GetFiles(path, "*.srt", SearchOption.AllDirectories));
        Regex languageRegex = new Regex(@"\.(?<lang>[a-z]{2,3})\.srt$", RegexOptions.IgnoreCase);

        var subtitleTasks = files.Select(file =>
        {
            var fileName = Path.GetFileName(file);
            var languageMatch = languageRegex.Match(fileName);

            string language = languageMatch.Success ? languageMatch.Groups["lang"].Value : "unknown";

            return Task.FromResult(new Subtitles
            {
                Path = file,
                FileName = languageMatch.Success
                    ? fileName.Substring(0, languageMatch.Index)
                    : Path.GetFileNameWithoutExtension(fileName),
                Language = language
            });
        });

        var srtFiles = await Task.WhenAll(subtitleTasks);
        return srtFiles.ToList();
    }

    /// <inheritdoc />
    public async Task<List<SubtitleItem>> ReadSubtitles(string filePath)
    {
        var parser = new SubRipParser();
        await using var fileStream = File.OpenRead(filePath);
        return parser.ParseStream(fileStream, Encoding.UTF8);
    }

    /// <inheritdoc />
    public async Task WriteSubtitles(string filePath, List<SubtitleItem> subtitles)
    {
        var writer = new SubRipWriter();
        await using var fileStream = File.OpenWrite(filePath);
        await writer.WriteStreamAsync(fileStream, subtitles);
    }

    /// <inheritdoc />
    public string CreateFilePath(string originalPath, string targetLanguage)
    {
        const string pattern = @"(\.([a-zA-Z]{2,3}))?\.srt$";
        var replacement = $".{targetLanguage}.srt";
        return Regex.Replace(originalPath, pattern, replacement);
    }
}