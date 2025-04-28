using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Subtitle;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;
using SubtitleValidationOptions = Lingarr.Server.Models.SubtitleValidationOptions;

namespace Lingarr.Server.Services;

public class SubtitleService : ISubtitleService
{
    private static readonly string[] SupportedExtensions = [".srt", ".ssa", ".ass"];
    private static readonly string[] SupportedCaptions = ["sdh", "cc", "forced", "hi"];
    private static readonly char[] WhitespaceCharacters = [' ', '\t', '\n', '\r'];

    private static readonly CultureInfo[] Cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
    private readonly ILogger<SubtitleService> _logger;

    public SubtitleService(
        ILogger<SubtitleService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<Subtitles>> GetAllSubtitles(string path)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogInformation(
                "Failed to collect subtitles in path |Red|{Path}|/Red|. Try reindexing or verify that the media is correctly set up in the source system.",
                path);
            return Task.FromResult(new List<Subtitles>());
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
                else if (caption == "hi" && language == "")
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
    public async Task WriteSubtitles(string filePath, List<SubtitleItem> subtitles, bool stripSubtitleFormatting)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        ISubtitleWriter writer = extension switch
        {
            ".srt" => new SrtWriter(),
            ".ssa" or ".ass" => new SsaWriter(),
            _ => throw new NotSupportedException($"Subtitle format {extension} is not supported")
        };

        await using var fileStream = File.OpenWrite(filePath);
        await writer.WriteStreamAsync(fileStream, subtitles, stripSubtitleFormatting);
    }

    /// <inheritdoc />
    public string CreateFilePath(string originalPath, string targetLanguage)
    {
        var extension = Path.GetExtension(originalPath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        var parts = fileNameWithoutExtension.Split('.');
        var reversedParts = parts.Reverse().ToList();

        // Extract caption if present
        string? caption = null;
        int captionIndex = reversedParts.FindIndex(p => SupportedCaptions.Contains(p.ToLower()));
        if (captionIndex != -1)
        {
            caption = reversedParts[captionIndex].ToLowerInvariant();
            reversedParts.RemoveAt(captionIndex);
        }

        // Extract language
        int languageIndex = reversedParts.FindIndex(part => TryGetLanguageByPart(part, out _));
        if (languageIndex != -1)
        {
            reversedParts.RemoveAt(languageIndex);
        }

        // Resolve target language code
        if (!TryGetLanguageByPart(targetLanguage, out var targetLanguageCode))
        {
            targetLanguageCode = targetLanguage;
        }

        // Reconstruct base parts
        var baseParts = reversedParts.AsEnumerable().Reverse().ToList();
        var newParts = new List<string>(baseParts);
        if (targetLanguageCode != null)
        {
            newParts.Add(targetLanguageCode.ToLowerInvariant());
        }

        // Add caption if present
        if (!string.IsNullOrEmpty(caption))
        {
            newParts.Add(caption);
        }

        // Build new file name and path
        var newFileName = string.Join(".", newParts) + extension;
        var directory = Path.GetDirectoryName(originalPath) ?? "";
        return Path.Combine(directory, newFileName);
    }

    /// <inheritdoc />
    public List<SubtitleItem> FixOverlappingSubtitles(List<SubtitleItem> subtitles)
    {
        const int buffer = 20;
        const int baseMinDuration = 1500;
        const int maxDuration = 6000;
        const double wordsPerSecond = 2.5;
        var fixCount = 0;

        for (var index = 1; index < subtitles.Count - 1; index++)
        {
            var prev = subtitles[index - 1];
            var current = subtitles[index];
            var next = subtitles[index + 1];

            var wordCount = CountWords(current.Lines);
            var optimalDuration = CalculateOptimalDuration(wordCount, wordsPerSecond, baseMinDuration, maxDuration);

            var hasOverlap = current.EndTime + buffer > next.StartTime;
            var needsAdjustment = hasOverlap;
            if (!needsAdjustment)
            {
                continue;
            }

            var currentDuration = current.EndTime - current.StartTime;

            var overlapTime = Math.Max(0, current.EndTime + buffer - next.StartTime);
            var optimalTimeNeeded = Math.Max(0, optimalDuration - currentDuration);
            var timeNeeded = Math.Max(overlapTime, optimalTimeNeeded);

            if (timeNeeded <= 0)
            {
                continue;
            }

            var prevWordCount = CountWords(prev.Lines);
            var nextWordCount = CountWords(next.Lines);
            var prevMinDuration = CalculateOptimalDuration(prevWordCount, wordsPerSecond, baseMinDuration, maxDuration);
            var nextMinDuration = CalculateOptimalDuration(nextWordCount, wordsPerSecond, baseMinDuration, maxDuration);

            var prevDuration = prev.EndTime - prev.StartTime;
            var nextDuration = next.EndTime - next.StartTime;
            var availableFromPrev = Math.Max(0, prevDuration - prevMinDuration);
            var availableFromNext = Math.Max(0, nextDuration - nextMinDuration);

            var timeFromPrev = Math.Min(timeNeeded / 2, availableFromPrev);
            var timeFromNext = Math.Min(timeNeeded - timeFromPrev, availableFromNext);

            var remainingNeeded = timeNeeded - timeFromPrev - timeFromNext;

            if (timeFromPrev > 0)
            {
                prev.EndTime -= timeFromPrev;
                current.StartTime -= timeFromPrev;
            }

            if (timeFromNext > 0)
            {
                next.StartTime += timeFromNext;
            }

            // If we still have overlap, adjust current subtitle timing
            switch (remainingNeeded)
            {
                case > 0 when overlapTime > 0:
                    current.EndTime = next.StartTime - buffer;
                    Console.WriteLine(
                        $"Couldn't reach optimal duration for subtitle #{current.Position} due to timing constraints");
                    break;
                case > 0 when optimalTimeNeeded > 0:
                    Console.WriteLine(
                        $"Subtitle #{current.Position} couldn't reach optimal duration of {optimalDuration}ms, achieved {current.EndTime - current.StartTime}ms");
                    break;
                default:
                {
                    if (overlapTime > 0)
                    {
                        current.EndTime = next.StartTime - buffer;
                    }
                    else if (optimalTimeNeeded > 0)
                    {
                        current.EndTime = current.StartTime + optimalDuration;
                    }

                    break;
                }
            }

            fixCount++;
            Console.WriteLine(
                $"Timing adjusted for subtitle #{current.Position} based on content length ({wordCount} words)");
        }

        if (subtitles.Count > 1)
        {
            var first = subtitles[0];
            var second = subtitles[1];
            var firstWordCount = CountWords(first.Lines);
            var firstOptimalDuration =
                CalculateOptimalDuration(firstWordCount, wordsPerSecond, baseMinDuration, maxDuration);

            var firstDuration = first.EndTime - first.StartTime;
            var hasOverlap = first.EndTime + buffer > second.StartTime;
            var isTooShort = firstDuration < firstOptimalDuration;

            if (hasOverlap || isTooShort)
            {
                var availableForward = Math.Max(0, second.StartTime - buffer - first.EndTime);

                if (first.EndTime + buffer > second.StartTime)
                {
                    first.EndTime = second.StartTime - buffer;
                    fixCount++;
                    Console.WriteLine(
                        $"Adjusted first subtitle #{first.Position} to avoid overlap with #{second.Position}");
                }
                else if (availableForward > 0 && (first.EndTime - first.StartTime) < firstOptimalDuration)
                {
                    var extensionNeeded = firstOptimalDuration - (first.EndTime - first.StartTime);
                    var extension = Math.Min(extensionNeeded, availableForward);
                    first.EndTime += extension;
                    fixCount++;
                    Console.WriteLine(
                        $"Extended first subtitle #{first.Position} duration based on content length ({firstWordCount} words)");
                }
            }

            // Last subtitle
            var lastIndex = subtitles.Count - 1;
            var last = subtitles[lastIndex];
            var secondLast = subtitles[lastIndex - 1];
            if (secondLast.EndTime + buffer > last.StartTime)
            {
                last.StartTime = secondLast.EndTime + buffer;

                if (last.EndTime - last.StartTime < baseMinDuration)
                {
                    last.EndTime = last.StartTime + baseMinDuration;
                }

                fixCount++;
                Console.WriteLine(
                    $"Adjusted last subtitle #{last.Position} to avoid overlap with #{secondLast.Position}");
            }
        }

        Console.WriteLine($"Fixed {fixCount} subtitle timings with content aware adjustments");
        return subtitles;
    }

    public bool ValidateSubtitle(string filePath, SubtitleValidationOptions options)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning("Cannot validate non-existent subtitle file: {FilePath}", filePath);
            return false;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return ValidateSubtitleStream(stream, Encoding.UTF8, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subtitle file: {FilePath}", filePath);
            return false;
        }
    }

    private bool ValidateSubtitleStream(
        Stream subtitleStream,
        Encoding encoding,
        SubtitleValidationOptions options)
    {
        try
        {
            if (!subtitleStream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable to validate file size.");
            }

            // Validate file size
            if (subtitleStream.Length > options.MaxFileSizeBytes)
            {
                _logger.LogWarning("Subtitle file exceeds maximum size of {MaxSize} bytes", options.MaxFileSizeBytes);
                return false;
            }

            // Reset stream position to the beginning before parsing
            subtitleStream.Seek(0, SeekOrigin.Begin);

            var parser = new SrtParser();
            var subtitles = parser.ParseStream(subtitleStream, encoding);

            if (subtitles == null || subtitles.Count < 2)
            {
                _logger.LogWarning("Subtitle file contains less than 2 valid subtitles");
                return false;
            }

            var expectedPosition = 1;
            SubtitleItem previousItem = null;

            foreach (var item in subtitles)
            {
                // Validate subtitle exists and has text
                if (item == null)
                {
                    _logger.LogWarning("Found null subtitle item");
                    return false;
                }

                // Choose the appropriate text content based on stripSubtitleFormatting
                List<string> contentLines = options.StripSubtitleFormatting ? item.PlaintextLines : item.Lines;

                if (contentLines == null || contentLines.Count == 0 || contentLines.All(string.IsNullOrWhiteSpace))
                {
                    _logger.LogWarning("Subtitle at position {Position} has no content", item.Position);
                    return false;
                }

                // Get the combined text for length checks
                string combinedText = string.Join(" ", contentLines);

                // Check that subtitle has at least the minimum length
                string trimmedText = combinedText.Trim();
                if (trimmedText.Length < options.MinSubtitleLength)
                {
                    _logger.LogWarning(
                        "Subtitle at position {Position} is too short. Length: {Length}, Minimum: {MinLength}",
                        item.Position, trimmedText.Length, options.MinSubtitleLength);
                    return false;
                }

                // Check sequence number/position
                if (item.Position != expectedPosition)
                {
                    _logger.LogWarning("Subtitle position mismatch. Expected: {Expected}, Found: {Found}",
                        expectedPosition, item.Position);
                    return false;
                }

                expectedPosition++;

                // Validate timing
                if (item.StartTime >= item.EndTime)
                {
                    _logger.LogWarning(
                        "Subtitle at position {Position} has invalid timing. StartTime: {StartTime}, EndTime: {EndTime}",
                        item.Position, item.StartTime, item.EndTime);
                    return false;
                }

                // Validate text length
                if (combinedText.Length > options.MaxSubtitleLength)
                {
                    _logger.LogWarning(
                        "Subtitle at position {Position} exceeds maximum length. Length: {Length}, Maximum: {MaxLength}",
                        item.Position, combinedText.Length, options.MaxSubtitleLength);
                    return false;
                }

                // Check for realistic durations
                var durationMs = item.EndTime - item.StartTime;
                if (durationMs < options.MinDurationMs || durationMs > options.MaxDurationSecs * 1000)
                {
                    _logger.LogWarning(
                        "Subtitle at position {Position} has unrealistic duration: {Duration}ms. Valid range: {MinDuration}ms to {MaxDuration}ms",
                        item.Position, durationMs, options.MinDurationMs, options.MaxDurationSecs * 1000);
                    return false;
                }

                // Check for overlapping with previous subtitle
                if (previousItem != null && item.StartTime < previousItem.EndTime)
                {
                    _logger.LogWarning(
                        "Subtitle at position {Position} overlaps with previous subtitle. Current start: {CurrentStart}, Previous end: {PreviousEnd}",
                        item.Position, item.StartTime, previousItem.EndTime);
                    return false;
                }

                // Check for control characters
                if (contentLines.Any(line => line.Any(c => char.IsControl(c) && c != '\n' && c != '\r')))
                {
                    _logger.LogWarning("Subtitle at position {Position} contains invalid control characters",
                        item.Position);
                    return false;
                }

                previousItem = item;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subtitle validation failed");
            return false;
        }
    }

    /// <summary>
    /// Counts the number of words in a list of plaintext subtitle lines
    /// </summary>
    /// <param name="lines">The plaintext subtitle lines to analyze</param>
    /// <returns>The total count of words across all lines</returns>
    private static int CountWords(List<string> lines)
    {
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Sum(line => line.Split(WhitespaceCharacters, StringSplitOptions.RemoveEmptyEntries).Length);
    }

    /// <summary>
    /// Calculates the optimal duration for a subtitle based on its word count
    /// </summary>
    /// <param name="wordCount">Number of words in the subtitle</param>
    /// <param name="wordsPerSecond">Reading speed in words per second</param>
    /// <param name="minDuration">Minimum allowed duration in milliseconds</param>
    /// <param name="maxDuration">Maximum allowed duration in milliseconds</param>
    /// <returns>The calculated optimal duration in milliseconds</returns>
    private static int CalculateOptimalDuration(int wordCount, double wordsPerSecond, int minDuration, int maxDuration)
    {
        var readingTime = (int)(wordCount * 1000 / wordsPerSecond);
        var optimalTime = readingTime + 500;
        return Math.Max(minDuration, Math.Min(optimalTime, maxDuration));
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