using System.Reflection;
using System.Text;
using Lingarr.Contracts.Translation;
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

    private readonly ILogger<SubtitleService> _logger;
    private readonly LanguageCodeService _languageCodeService;

    public SubtitleService(
        ILogger<SubtitleService> logger,
        LanguageCodeService languageCodeService)
    {
        _logger = logger;
        _languageCodeService = languageCodeService;
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
    public string CreateFilePath(string originalPath, string targetLanguage, string subtitleTag)
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
        string? targetLanguageCode = null;
        if (!string.IsNullOrEmpty(targetLanguage)
            && !TryGetLanguageByPart(targetLanguage, out targetLanguageCode))
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
        
        // Add tag if provided
        if (!string.IsNullOrEmpty(subtitleTag))
        {
            newParts.Add(subtitleTag.ToLowerInvariant());
        }
        
        // Build new file name and path
        var newFileName = string.Join(".", newParts) + extension;
        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        return Path.Combine(directory, newFileName);
    }

    /// <inheritdoc />
    public List<SubtitleItem> FixOverlappingSubtitles(List<SubtitleItem> subtitles)
    {
        const int gap = 20;
        const int minimumDuration = 500;
        var fixCount = 0;

        for (var index = 0; index < subtitles.Count - 1; index++)
        {
            var current = subtitles[index];
            var next = subtitles[index + 1];

            if (current.EndTime + gap <= next.StartTime)
            {
                continue;
            }

            var trimmedEndTime = next.StartTime - gap;
            if (trimmedEndTime - current.StartTime < minimumDuration)
            {
                // Intentionally stacked cues, such as dual-speaker dialogue sharing a
                // timestamp, would become unreadable when trimmed. Keep their timing.
                continue;
            }

            current.EndTime = trimmedEndTime;
            fixCount++;
            _logger.LogDebug("Trimmed subtitle #{Position} to remove overlap with #{NextPosition}",
                current.Position, next.Position);
        }

        _logger.LogInformation("Fixed {FixCount} overlapping subtitles by trimming end times", fixCount);
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

            if (subtitles.Count < 2)
            {
                _logger.LogWarning("Subtitle file contains less than 2 valid subtitles");
                return false;
            }

            var expectedPosition = 1;
            SubtitleItem previousItem = null;

            foreach (var item in subtitles)
            {
                // Choose the appropriate text content based on stripSubtitleFormatting
                List<string> contentLines = options.StripSubtitleFormatting ? item.PlaintextLines : item.Lines;

                if (contentLines.Count == 0 || contentLines.All(string.IsNullOrWhiteSpace))
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
    /// Adds an introductory subtitle at the beginning that identifies the translation service used.
    /// The intro duration is automatically adjusted to avoid overlapping with existing subtitles.
    /// </summary>
    /// <param name="serviceType">The translation service type (e.g., "openai", "google").</param>
    /// <param name="translatedSubtitles">The subtitle list to prepend the intro to.</param>
    /// <param name="translationService">The service instance used to extract model name if available.</param>
    public void AddTranslatorInfo(string serviceType, List<SubtitleItem> translatedSubtitles,
        ITranslationService translationService)
    {
        // Check if the service has a ModelName property
        var serviceName = char.ToUpper(serviceType[0]) + serviceType[1..];

        var modelField = translationService.GetType().GetField("_model",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (modelField != null)
        {
            var modelName = modelField.GetValue(translationService)?.ToString();
            if (!string.IsNullOrEmpty(modelName))
            {
                serviceName += " - " + modelName;
            }
        }

        var introText = $"# Translated with Lingarr using {serviceName} translator #";
        var introDuration = 5.0; // Default duration in seconds

        // Check if there are existing subtitles and if the first one starts before our intro ends
        if (translatedSubtitles.Count > 0)
        {
            var firstSubtitle = translatedSubtitles[0];
            var firstSubtitleStartTimeSeconds = firstSubtitle.StartTime / 1000.0;

            // If the first subtitle starts before our intro would end, adjust the intro duration
            if (firstSubtitleStartTimeSeconds < introDuration)
            {
                // Leave a small gap (e.g., 0.5 seconds) between intro and first subtitle
                introDuration = Math.Max(0.5, firstSubtitleStartTimeSeconds - 0.5);
                _logger.LogInformation(
                    "Adjusted intro duration to {introDuration} seconds to avoid overlap with first subtitle at {firstStart} seconds",
                    introDuration, firstSubtitleStartTimeSeconds);
            }
        }

        var introSubtitle = new SubtitleItem
        {
            StartTime = 0,
            EndTime = (int)(introDuration * 1000),
            Lines = [introText],
            PlaintextLines = [introText],
            TranslatedLines = [introText],
            // SsaWriter reads items[0].SsaFormat for the header — carry it over.
            SsaFormat = translatedSubtitles.FirstOrDefault()?.SsaFormat
        };

        translatedSubtitles.Insert(0, introSubtitle);
    }

    /// <inheritdoc />
    public async Task<List<Subtitles>> GetSubtitles(string path, string fileName)
    {
        var allSubtitles = await GetAllSubtitles(path);
        return allSubtitles
            .Where(s => s.FileName.StartsWith(fileName + ".") || s.FileName == fileName)
            .ToList();
    }

    /// <inheritdoc />
    public SelectedSourceSubtitle? SelectSourceSubtitle(
        List<Subtitles> matchingSubtitles,
        HashSet<string> sourceCodes,
        string ignoreCaptions)
    {
        var availableLanguages = matchingSubtitles
            .Select(s => s.Language.ToLowerInvariant())
            .ToHashSet();

        var sourceLanguage = availableLanguages.FirstOrDefault(lang => sourceCodes.Contains(lang));
        if (sourceLanguage == null)
        {
            return null;
        }

        var sourceSubtitle = ignoreCaptions == "true"
            ? matchingSubtitles.FirstOrDefault(s => s.Language == sourceLanguage && string.IsNullOrEmpty(s.Caption))
                ?? matchingSubtitles.FirstOrDefault(s => s.Language == sourceLanguage)
            : matchingSubtitles.FirstOrDefault(s => s.Language == sourceLanguage);

        if (sourceSubtitle == null)
        {
            return null;
        }

        return new SelectedSourceSubtitle
        {
            Subtitle = sourceSubtitle,
            SourceLanguage = sourceLanguage,
            AvailableLanguages = availableLanguages
        };
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
    /// matched culture; otherwise, null if no match is found.
    /// </param>
    private bool TryGetLanguageByPart(string part, out string? languageCode)
    {
        languageCode = null;
        if (!_languageCodeService.Validate(part))
        {
            return false;
        }
        
        try
        {
            languageCode = LanguageCodeService.GetNormalizedCode(part);
            return true;
        }
        catch (ArgumentException)
        {
            // Should not happen as we validated
            return false;
        }
    }
}