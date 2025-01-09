using System.Globalization;
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
    private static readonly string[] SupportedCaptions = { "sdh", "cc", "forced", "hi" };

    private static readonly CultureInfo[] Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

    /// <inheritdoc />
    public Task<List<Subtitles>> GetAllSubtitles(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        var subtitles = new List<Subtitles>();
        foreach (var extension in SupportedExtensions)
        {
            var files = Directory.GetFiles(path, $"*{extension}", SearchOption.AllDirectories);

            var subtitleFiles = files.Select(file =>
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('.').Reverse().ToList();
                var language = "";
                var caption = "";

                // First look for caption
                var captionPart = parts.FirstOrDefault(p => SupportedCaptions.Contains(p.ToLower()));
                if (captionPart != null)
                {
                    caption = captionPart.ToLower();
                    parts.Remove(captionPart);
                }

                // Then look for language in remaining parts
                var languagePart = parts.FirstOrDefault(p => TryGetLanguageByPart(p, out var code));
                if (languagePart != null && TryGetLanguageByPart(languagePart, out var languageCode))
                {
                    language = languageCode;
                    parts.Remove(languagePart);
                }
                // Hindi is an exception, if we didn't find a language, and we did found Hindi, We set that as language
                else if(caption == "hi" && language == "")
                {
                    language = caption;
                    caption = "";
                }

                return new Subtitles
                {
                    Path = file,
                    FileName = fileName,
                    Language = language ?? "unknown",
                    Caption = caption,
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
    
    /// <summary>
    /// Tries to match the specified <paramref name="part"/> against any known culture/language
    /// and outputs the two-letter ISO language code if found.
    /// </summary>
    /// <param name="part">
    /// The string segment from the file name that may represent a culture or language code.
    /// </param>
    /// <param name="languageCode">
    /// When this method returns, contains the two-letter ISO language code corresponding to the 
    /// matched culture; otherwise, <c>null</c> if no match is found.
    /// </param>
    private static bool TryGetLanguageByPart(string part, out string? languageCode)
    {
        languageCode = null;
        var culture = Cultures.FirstOrDefault(c =>
            string.Equals(part, c.Name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(part, c.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(part, c.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase));

        if (culture != null)
        {
            languageCode = culture.TwoLetterISOLanguageName;
            return true;
        }

        return false;
    }
}