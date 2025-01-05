using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class SubtitleService : ISubtitleService
{
    private static readonly string[] SupportedExtensions = { ".srt", ".ssa", ".ass" };
    private static readonly Regex LanguageRegex = new(@"\.(?<format>[a-z]{2,3})(\.(?<language>[a-z]{2,3}))?\.(?:srt|ssa|ass)$", RegexOptions.IgnoreCase);
    
    /// <inheritdoc />
    public Task<List<Subtitles>> GetAllSubtitles(string path)
    {
        var subtitles = new List<Subtitles>();
        
        foreach (var extension in SupportedExtensions)
        {
            var files = Directory.GetFiles(path, $"*{extension}", SearchOption.AllDirectories);
            
            var subtitleFiles = files.Select(file =>
            {
                var match = LanguageRegex.Match(Path.GetFileName(file));
                return new Subtitles
                {
                    Path = file,
                    FileName = match.Success
                        ? Path.GetFileName(file)[..match.Index]
                        : Path.GetFileNameWithoutExtension(file),
                    Language = !match.Success ? "unknown" :
                        match.Groups["language"].Success ? $"{match.Groups["format"]}-{match.Groups["language"]}" :
                        match.Groups["format"].Value,
                    Format = extension
                };
            });
            
            subtitles.AddRange(subtitleFiles);
        }

        return Task.FromResult(subtitles);
    }

    /// <inheritdoc />
    public async Task<List<SubtitleItem>> ReadSubtitles(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        ISubtitleParser parser = extension switch
        {
            ".srt" => new SrtParser(),
            ".ssa" or ".ass" => new SsaParser(),
            _ => throw new NotSupportedException($"Subtitle format {extension} is not supported")
        };

        await using var fileStream = File.OpenRead(filePath);
        return parser.ParseStream(fileStream, Encoding.UTF8);
    }

    /// <inheritdoc />
    public async Task WriteSubtitles(string filePath, List<SubtitleItem> subtitles)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        ISubtitleWriter writer = extension switch
        {
            ".srt" => new SrtWriter(),
            ".ssa" or ".ass" => new SsaWriter(),
            _ => throw new NotSupportedException($"Subtitle format {extension} is not supported")
        };

        await using var fileStream = File.OpenWrite(filePath);
        await writer.WriteStreamAsync(fileStream, subtitles);
    }

    /// <inheritdoc />
    public string CreateFilePath(string originalPath, string targetLanguage)
    {
        var extension = Path.GetExtension(originalPath).ToLower();
        var pattern = @"(\.([a-zA-Z]{2,3}))?\." + extension[1..] + "$";
        var replacement = $".{targetLanguage}{extension}";
        return Regex.Replace(originalPath, pattern, replacement);
    }
}