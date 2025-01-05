using System.Security.Cryptography;
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
    private static readonly string[] SupportedExtensions = { ".srt", ".ssa", ".ass" };
    private readonly ITranslationRequestService _translationRequestService;
    private readonly ILogger<IMediaSubtitleProcessor> _logger;
    private readonly ISettingService _settingService;
    private readonly LingarrDbContext _dbContext;
    private string _hash = string.Empty;
    private IMedia _media = null!;
    private MediaType _mediaType;

    public MediaSubtitleProcessor(
        ITranslationRequestService translationRequestService,
        ILogger<IMediaSubtitleProcessor> logger,
        ISettingService settingService,
        LingarrDbContext dbContext)
    {
        _translationRequestService = translationRequestService;
        _settingService = settingService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessMedia(IMedia media, MediaType mediaType)
    {
        var files = await EnumerateSubtitleFiles(media.Path, media.FileName);
        _media = media;
        _mediaType = mediaType;
        _hash = CreateHash(files);

        if (string.IsNullOrEmpty(media.MediaHash) || media.MediaHash != _hash)
        {
            _logger.LogInformation("Initiating subtitle processing.");
            return await ProcessSubtitles(files);
        }

        return false;
    }

    /// <summary>
    /// Enumerates all subtitle files for a given media file.
    /// </summary>
    /// <param name="path">The directory path where the media file is located.</param>
    /// <param name="filename">The name of the media file.</param>
    /// <returns>A list of paths to subtitle files matching the media filename.</returns>
    private async Task<List<string>> EnumerateSubtitleFiles(string path, string filename)
    {
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        return await Task.Run(() =>
            SupportedExtensions
                .SelectMany(ext =>
                    Directory.EnumerateFiles(path, $"{filenameWithoutExtension}*{ext}", SearchOption.TopDirectoryOnly))
                .ToList());
    }

    /// <summary>
    /// Creates a hash of the current subtitle file state.
    /// </summary>
    /// <param name="files">List of subtitle file paths to include in the hash.</param>
    /// <returns>A Base64 encoded string representing the hash of the current subtitle state.</returns>
    private string CreateHash(List<string> files)
    {
        using var sha256 = SHA256.Create();
        var hashInput = string.Join("|", files.OrderBy(f => f));
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Processes subtitle files for translation based on configured languages.
    /// </summary>
    /// <param name="subtitleFiles">List of subtitle files to process.</param>
    /// <returns>True if new translation requests were created, false otherwise.</returns>
    private async Task<bool> ProcessSubtitles(List<string> subtitleFiles)
    {
        var existingLanguages = ExtractLanguageCodes(subtitleFiles);
        var sourceLanguages = await GetLanguagesSetting<SourceLanguage>("source_languages");
        var targetLanguages = await GetLanguagesSetting<TargetLanguage>("target_languages");

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
            var sourceSubtitleFile = subtitleFiles.FirstOrDefault(file =>
                Path.GetFileNameWithoutExtension(file)
                    .EndsWith($".{sourceLanguage}", StringComparison.OrdinalIgnoreCase));

            if (sourceSubtitleFile != null)
            {
                var subtitleFormat = Path.GetExtension(sourceSubtitleFile).TrimStart('.').ToLower();

                foreach (var targetLanguage in targetLanguages.Except(existingLanguages))
                {
                    var translationId = await _translationRequestService.CreateRequest(new TranslateAbleSubtitle
                    {
                        MediaId = _media.Id,
                        MediaType = _mediaType,
                        SubtitlePath = sourceSubtitleFile,
                        TargetLanguage = targetLanguage,
                        SourceLanguage = sourceLanguage,
                        SubtitleFormat = subtitleFormat
                    });
                    _logger.LogInformation("Initiating translation for {subtitleFile} under {translationId}",
                        sourceSubtitleFile,
                        translationId);
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
    /// Retrieves language settings from the application configuration.
    /// </summary>
    /// <typeparam name="T">The type of language setting to retrieve (Source or Target).</typeparam>
    /// <param name="settingName">The name of the setting to retrieve.</param>
    /// <returns>A HashSet of language codes from the configuration.</returns>
    private async Task<HashSet<string>> GetLanguagesSetting<T>(string settingName) where T : class, ILanguage
    {
        var languages = await _settingService.GetSettingAsJson<T>(settingName);

        return languages
            .Where(lang => IsValidLanguageCode(lang.Code))
            .Select(lang => lang.Code)
            .ToHashSet();
    }

    /// <summary>
    /// Extracts language codes from subtitle file names.
    /// </summary>
    /// <param name="subtitleFiles">List of subtitle file paths to process.</param>
    /// <returns>A HashSet of valid language codes found in the file names.</returns>
    private HashSet<string> ExtractLanguageCodes(List<string> subtitleFiles)
    {
        return subtitleFiles
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Select(fileName => fileName.Split('.').LastOrDefault())
            .Where(langCode => IsValidLanguageCode(langCode))
            .Select(langCode => langCode!.ToLowerInvariant())
            .ToHashSet();
    }

    /// <summary>
    /// Validates a language code string.
    /// </summary>
    /// <param name="code">The language code to validate.</param>
    /// <returns>True if the code is a valid two-letter language code, false otherwise.</returns>
    private bool IsValidLanguageCode(string? code)
    {
        return !string.IsNullOrEmpty(code) && code.Length == 2;
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