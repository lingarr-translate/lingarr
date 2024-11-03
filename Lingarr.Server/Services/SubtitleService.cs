using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class SubtitleService : ISubtitleService
{
    /// <inheritdoc />
    public Task<List<Subtitles>> GetAllSubtitles(string path)
    {
        var files = Directory.GetFiles(path, "*.srt", SearchOption.AllDirectories);
        var regex = new Regex(@"\.(?<format>[a-z]{2,3})(\.(?<language>[a-z]{2,3}))?\.srt$", RegexOptions.IgnoreCase);

        var subtitles = files.Select(file =>
        {
            var match = regex.Match(Path.GetFileName(file));
            return new Subtitles
            {
                Path = file,
                FileName = match.Success
                    ? Path.GetFileName(file)[..match.Index]
                    : Path.GetFileNameWithoutExtension(file),
                Language = !match.Success ? "unknown" :
                    match.Groups["language"].Success ? $"{match.Groups["format"]}-{match.Groups["language"]}" :
                    match.Groups["format"].Value
            };
        }).ToList();

        return Task.FromResult(subtitles);
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