using System.Security.Cryptography;
using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Interfaces;
using Lingarr.Server.Interfaces;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class MediaSubtitleProcessor
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly LingarrDbContext _dbContext;
    private readonly ILogger<MediaSubtitleProcessor> _logger;
    private readonly ISettingService _settingService;
    private string _hash = string.Empty;
    private IMedia? _media;
    
    public MediaSubtitleProcessor(
        IBackgroundJobClient backgroundJobClient,
        LingarrDbContext dbContext, 
        ILogger<MediaSubtitleProcessor> logger,
        ISettingService settingService)
    {
        _backgroundJobClient = backgroundJobClient;
        _settingService = settingService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> ProcessMediaAsync(IMedia media)
    {
        var files = await EnumerateSubtitleFilesAsync(media.Path, media.FileName);
        _media = media;
        _hash = CreateHash(files);

        if (string.IsNullOrEmpty(media.MediaHash) || media.MediaHash != _hash)
        {
            _logger.LogInformation("Initiating subtitle processing.");
            return await ProcessSubtitlesAsync(files);
        }

        return false;
    }

    private async Task<List<string>> EnumerateSubtitleFilesAsync(string path, string filename)
    {
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        return await Task.Run(() => Directory.EnumerateFiles($"media{path}", $"{filenameWithoutExtension}*.srt", SearchOption.TopDirectoryOnly)
                .ToList());
    }

    private string CreateHash(List<string> files)
    {
        using var sha256 = SHA256.Create();
        var hashInput = string.Join("|", files.OrderBy(f => f));
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes);
    }

    private async Task<bool> ProcessSubtitlesAsync(List<string> subtitleFiles)
    {
        var existingLanguages = ExtractLanguageCodes(subtitleFiles);
        var sourceLanguages = await GetLanguagesSetting<SourceLanguage>("source_languages");
        var targetLanguages = await GetLanguagesSetting<TargetLanguage>("target_languages");
        
        if (sourceLanguages.Count == 0 || targetLanguages.Count == 0)
        {
            _logger.LogWarning("Source or target languages are empty. Source languages: {SourceCount}, Target languages: {TargetCount}",
                sourceLanguages.Count, targetLanguages.Count);
            await UpdateHash();
            return false;
        }
        
        var sourceLanguage = existingLanguages.FirstOrDefault(lang => sourceLanguages.Contains(lang));
        if (sourceLanguage != null && targetLanguages.Any())
        {
            var sourceSubtitleFile = subtitleFiles.FirstOrDefault(file => 
                Path.GetFileNameWithoutExtension(file).EndsWith($".{sourceLanguage}", StringComparison.OrdinalIgnoreCase));

            if (sourceSubtitleFile != null)
            {
                foreach (var targetLang in targetLanguages.Except(existingLanguages))
                {
                    return InitiateTranslationAsync(sourceSubtitleFile, targetLang, sourceLanguage);
                }
                await UpdateHash();
            }
            else
            {
                _logger.LogWarning("No source subtitle file found for language: |Green|{SourceLanguage}|/Green|", sourceLanguage);
                await UpdateHash();
            }
        }
        else
        {
            _logger.LogWarning("No valid source language or target languages found for media |Green|{FileName}|/Green|. " +
                               "Existing languages: |Red|{ExistingLanguages}|/Red|, " +
                               "Source languages: |Red|{SourceLanguages}|/Red|, " +
                               "Target languages: |Red|{TargetLanguages}|/Red|", 
                string.Join(", ", _media?.FileName),
                string.Join(", ", existingLanguages),
                string.Join(", ", sourceLanguages),
                string.Join(", ", targetLanguages));
            await UpdateHash();
        }
        return false;
    }

    private async Task<HashSet<string>> GetLanguagesSetting<T>(string settingName) where T : class, ILanguage
    {
        var languages = await _settingService.GetSettingAsJson<T>(settingName);
        
        return languages
            .Where(lang => IsValidLanguageCode(lang.Code))
            .Select(lang => lang.Code)
            .ToHashSet();
    }

    private HashSet<string> ExtractLanguageCodes(List<string> subtitleFiles)
    {
        return subtitleFiles
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Select(fileName => fileName.Split('.').LastOrDefault())
            .Where(langCode => IsValidLanguageCode(langCode))
            .Select(langCode => langCode!.ToLowerInvariant())
            .ToHashSet();
    }

    private bool IsValidLanguageCode(string? code)
    {
        return !string.IsNullOrEmpty(code) && code.Length == 2;
    }

    private bool InitiateTranslationAsync(string subtitleFile, string targetLanguage, string sourceLanguage)
    {
        _logger.LogInformation("Initiating translation for {subtitleFile}", subtitleFile);
        Task.Run(() => _backgroundJobClient.Enqueue<TranslationJob>(job =>
            job.Execute(null, new TranslateAbleSubtitle
            {
                SubtitlePath = subtitleFile,
                TargetLanguage = targetLanguage,
                SourceLanguage = sourceLanguage
            }, CancellationToken.None)
        ));
        return true;
    }
    
    private async Task UpdateHash()
    {
        if (_media != null)
        {
            _media.MediaHash = _hash;
            _dbContext.Update(_media);
            await _dbContext.SaveChangesAsync();
        }
    }
}