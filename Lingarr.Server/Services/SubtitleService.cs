using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class SubtitleService : ISubtitleService
{
    /// <inheritdoc />
    public List<Subtitles> Collect(string path)
    {
        List<Subtitles> srtFiles = new List<Subtitles>();

        if (!path.StartsWith("/"))
        {
            path = $"/{path}";
        }

        string[] files = Directory.GetFiles($"media{path}", "*.srt", SearchOption.AllDirectories);
        Regex languageRegex = new Regex(@"\.(?<lang>[a-z]{2,3})\.srt$", RegexOptions.IgnoreCase);

        foreach (string file in files)
        {
            var fileName = Path.GetFileName(file);
            var languageMatch = languageRegex.Match(fileName);

            string language = languageMatch.Success ? languageMatch.Groups["lang"].Value : "unknown";

            srtFiles.Add(new Subtitles
            {
                Path = file,
                FileName = languageMatch.Success
                    ? fileName.Substring(0, languageMatch.Index)
                    : Path.GetFileNameWithoutExtension(file),
                Language = language
            });
        }

        return srtFiles;
    }
}