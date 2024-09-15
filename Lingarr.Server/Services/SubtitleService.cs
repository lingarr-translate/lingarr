using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class SubtitleService : ISubtitleService
{
    /// <inheritdoc />
    public async Task<List<Subtitles>> Collect(string path)
    {
        if (!path.StartsWith("/"))
        {
            path = $"/{path}";
        }

        string fullPath = $"media{path}";
        var files = await Task.Run(() => Directory.GetFiles(fullPath, "*.srt", SearchOption.AllDirectories));
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
}