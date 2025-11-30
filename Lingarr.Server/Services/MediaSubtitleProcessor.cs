using System.Security.Cryptography;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Enum;
using Lingarr.Core.Interfaces;
using Lingarr.Server.Interfaces;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class MediaSubtitleProcessor : IMediaSubtitleProcessor
{
    private readonly ITranslationRequestService _translationRequestService;
    private readonly ILogger<IMediaSubtitleProcessor> _logger;
    private readonly ISubtitleService _subtitleService;
    private readonly ISettingService _settingService;
    private readonly LingarrDbContext _dbContext;
    private string _hash = string.Empty;
    private IMedia _media = null!;
    private MediaType _mediaType;

    public MediaSubtitleProcessor(
        ITranslationRequestService translationRequestService,
        ILogger<IMediaSubtitleProcessor> logger,
        ISettingService settingService,
        ISubtitleService subtitleService,
        LingarrDbContext dbContext)
    {
        _translationRequestService = translationRequestService;
        _settingService = settingService;
        _subtitleService = subtitleService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessMedia(
        IMedia media, 
        MediaType mediaType)
    {
        if (media.Path == null)
        {
            return false;
        }
        var allSubtitles = await _subtitleService.GetAllSubtitles(media.Path);
        var matchingSubtitles = allSubtitles
            .Where(s => s.FileName.StartsWith(media.FileName + ".") || s.FileName == media.FileName)
            .ToList();

        if (!matchingSubtitles.Any())
        {
            return false;
        }

        var sourceLanguages = await GetLanguagesSetting<SourceLanguage>(SettingKeys.Translation.SourceLanguages);
        var targetLanguages = await GetLanguagesSetting<TargetLanguage>(SettingKeys.Translation.TargetLanguages);
        var ignoreCaptions = await _settingService.GetSetting(SettingKeys.Translation.IgnoreCaptions);

        _media = media;
        _mediaType = mediaType;
        _hash = CreateHash(matchingSubtitles, sourceLanguages, targetLanguages, ignoreCaptions);
        if (!string.IsNullOrEmpty(media.MediaHash) && media.MediaHash == _hash)
        {
            return false;
        }
        
        _logger.LogInformation("Initiating subtitle processing.");
        return await ProcessSubtitles(matchingSubtitles, sourceLanguages, targetLanguages, ignoreCaptions);
    }

    /// <summary>
    /// Processes subtitle files for translation based on configured languages.
    /// </summary>
    /// <param name="subtitles">List of subtitle files to process.</param>
    /// <param name="sourceLanguages">The source languages.</param>
    /// <param name="targetLanguages">The target languages.</param>
    /// <param name="ignoreCaptions">The ignore captions setting.</param>
    /// <returns>True if new translation requests were created, false otherwise.</returns>
    private async Task<bool> ProcessSubtitles(
        List<Subtitles> subtitles,
        HashSet<string> sourceLanguages,
        HashSet<string> targetLanguages,
        string ignoreCaptions)
    {
        var existingLanguages = ExtractLanguageCodes(subtitles);

        if (sourceLanguages.Count == 0 || targetLanguages.Count == 0)
        {
            _logger.LogWarning(
                "Source or target languages are empty. Source languages: {SourceCount}, Target languages: {TargetCount}",
                sourceLanguages.Count, targetLanguages.Count);
            await UpdateHash();
            return false;
        }

        var sourceLanguage = existingLanguages.FirstOrDefault(lang => sourceLanguages.Contains(lang));
        if (sourceLanguage != null && targetLanguages.Any())
        {
            // If ignoreCaptions is disabled, prefer non-caption source subtitles, but fallback to caption subtitles if no alternative exists
            var sourceSubtitle = ignoreCaptions == "true"
                ? subtitles.FirstOrDefault(s => s.Language == sourceLanguage && string.IsNullOrEmpty(s.Caption)) 
                    ?? subtitles.FirstOrDefault(s => s.Language == sourceLanguage)
                : subtitles.FirstOrDefault(s => s.Language == sourceLanguage);
                
            if (sourceSubtitle != null)
            {
                // Get languages that don't yet exist to validate whether captions in those languages are available
                var languagesToTranslate = targetLanguages.Except(existingLanguages);
                if (ignoreCaptions == "true")
                {
                    var targetLanguagesWithCaptions = subtitles
                        .Where(s => targetLanguages.Contains(s.Language) && !string.IsNullOrEmpty(s.Caption))
                        .Select(s => s.Language)
                        .Distinct()
                        .ToList();

                    if (targetLanguagesWithCaptions.Any())
                    {
                        _logger.LogInformation(
                            "Translation skipped because captions exist for target languages: |Green|{CaptionLanguages}|/Green| and ignoreCaptions is disabled",
                            string.Join(", ", targetLanguagesWithCaptions));
                        await UpdateHash();
                        return false;
                    }
                }

                foreach (var targetLanguage in languagesToTranslate)
                {
                    await _translationRequestService.CreateRequest(new TranslateAbleSubtitle
                    {
                        MediaId = _media.Id,
                        MediaType = _mediaType,
                        SubtitlePath = sourceSubtitle.Path,
                        TargetLanguage = targetLanguage,
                        SourceLanguage = sourceLanguage,
                        SubtitleFormat = sourceSubtitle.Format
                    });
                    _logger.LogInformation(
                        "Initiating translation from |Orange|{sourceLanguage}|/Orange| to |Orange|{targetLanguage}|/Orange| for |Green|{subtitleFile}|/Green|",
                        sourceLanguage,
                        targetLanguage,
                        sourceSubtitle.Path);
                }

                await UpdateHash();
                return true;
            }

            _logger.LogWarning("No source subtitle file found for language: |Green|{SourceLanguage}|/Green|",
                sourceLanguage);

            await UpdateHash();
            return false;
        }

        _logger.LogWarning(
            "No valid source language or target languages found for media |Green|{FileName}|/Green|. " +
            "Existing languages: |Red|{ExistingLanguages}|/Red|, " +
            "Source languages: |Red|{SourceLanguages}|/Red|, " +
            "Target languages: |Red|{TargetLanguages}|/Red|",
            string.Join(", ", _media?.FileName),
            string.Join(", ", existingLanguages),
            string.Join(", ", sourceLanguages),
            string.Join(", ", targetLanguages));

        await UpdateHash();
        return false;
    }

    /// <summary>
    /// Creates a hash of the current subtitle file state.
    /// </summary>
    /// <param name="subtitles">List of subtitle file paths to include in the hash.</param>
    /// <param name="sourceLanguages">The source languages.</param>
    /// <param name="targetLanguages">The target languages.</param>
    /// <param name="ignoreCaptions">The ignore captions setting.</param>
    /// <returns>A Base64 encoded string representing the hash of the current subtitle state.</returns>
    private string CreateHash(
        List<Subtitles> subtitles,
        HashSet<string> sourceLanguages,
        HashSet<string> targetLanguages,
        string ignoreCaptions)
    {
        using var sha256 = SHA256.Create();
        var subtitlePaths = string.Join("|", subtitles.Select(subtitle => subtitle.Path)
            .ToList()
            .OrderBy(f => f));
        
        var sourceLangs = string.Join(",", sourceLanguages.OrderBy(l => l));
        var targetLangs = string.Join(",", targetLanguages.OrderBy(l => l));
        
        var hashInput = $"{subtitlePaths}|{sourceLangs}|{targetLangs}|{ignoreCaptions}";
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Extracts language codes from subtitle file names.
    /// </summary>
    /// <param name="subtitles">List of subtitle file paths to process.</param>
    /// <returns>A HashSet of valid language codes found in the file names.</returns>
    private HashSet<string> ExtractLanguageCodes(List<Subtitles> subtitles)
    {
        return subtitles
            .Select(s => s.Language.ToLowerInvariant())
            .ToHashSet();
    }

    /// <summary>
    /// Retrieves language settings from the application configuration.
    /// </summary>
    /// <typeparam name="T">The type of language setting to retrieve (Source or Target).</typeparam>
    /// <param name="settingName">The name of the setting to retrieve.</param>
    /// <returns>A HashSet of language codes from the configuration.</returns>
    private async Task<HashSet<string>> GetLanguagesSetting<T>(string settingName) where T : class, ILanguage
    {
        var languages = await _settingService.GetSettingAsJson<T>(settingName);
        return languages
            .Select(lang => lang.Code)
            .ToHashSet();
    }

    /// <summary>
    /// Updates the media hash in the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns
    private async Task UpdateHash()
    {
        _media.MediaHash = _hash;
        _dbContext.Update(_media);
        await _dbContext.SaveChangesAsync();
    }
}