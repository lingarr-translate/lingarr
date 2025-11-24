using DeepL;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Services.Translation.Base;
using Microsoft.Extensions.Caching.Memory;

namespace Lingarr.Server.Services.Translation;

public class DeepLService : BaseTranslationService
{
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions());
    private const string SourceLanguagesCacheKey = "DeepL_SourceLanguages";
    private const string TargetLanguagesCacheKey = "DeepL_TargetLanguages";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private const string FilteredLanguageCode = "pt";

    private ITranslator? _translator;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    
    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;
    
    public DeepLService(
        ISettingService settings,
        ILogger<DeepLService> logger) : base(settings, logger)
    {
    }

    /// <summary>
    /// Initializes the translation service with necessary configurations and credentials.
    /// This method is thread-safe and ensures one-time initialization of service dependencies.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when required configuration settings are missing or invalid</exception>
    private async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            await _initLock.WaitAsync();
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.DeepL.DeeplApiKey,
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier
            ]);

            var authKey = settings[SettingKeys.Translation.DeepL.DeeplApiKey];
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new InvalidOperationException("DeepL failed, please validate the API key.");
            }

            _maxRetries = int.TryParse(settings[SettingKeys.Translation.MaxRetries], out var maxRetries) 
                ? maxRetries 
                : 3;
            
            var retryDelaySeconds = int.TryParse(settings[SettingKeys.Translation.RetryDelay], out var delaySeconds) 
                ? delaySeconds 
                : 2;
            _retryDelay = TimeSpan.FromSeconds(retryDelaySeconds);

            _retryDelayMultiplier = int.TryParse(settings[SettingKeys.Translation.RetryDelayMultiplier], out var multiplier) 
                ? multiplier 
                : 2;

            _translator = new Translator(authKey, new TranslatorOptions
            {
                MaximumNetworkRetries = 3,
                PerRetryConnectionTimeout = TimeSpan.FromSeconds(10)
            });

            var usage = await _translator.GetUsageAsync();
            if (usage.AnyLimitReached)
            {
                throw new InvalidOperationException("Translation using DeepL failed, usage limit reached.");
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
    
    /// <inheritdoc />
    public override async Task<List<SourceLanguage>> GetLanguages()
    {
        _logger.LogInformation($"Retrieving |Green|DeepL|/Green| languages");
        await InitializeAsync();
        
        if (_translator == null)
        {
            throw new InvalidOperationException("DeepL translator was not properly initialized.");
        }

        var targetLanguages = await GetTargetLanguages(_translator);
        if (targetLanguages == null)
        {
            throw new InvalidOperationException("Failed to retrieve target languages from DeepL");
        }
        var sourceLanguages = await GetSourceLanguages(_translator);
        if (sourceLanguages == null)
        {
            throw new InvalidOperationException("Failed to retrieve source languages from DeepL");
        }
        
        var targetLanguagesList = targetLanguages
            .Where(lang => lang.Code != FilteredLanguageCode)
            .Select(lang => lang.Code)
            .ToList();
        
        return sourceLanguages
            .Where(lang => lang.Code != FilteredLanguageCode)
            .Select(lang => new SourceLanguage
        {
            Code = lang.Code,
            Name = lang.Name,
            Targets = targetLanguagesList
        }).ToList();
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage, 
        List<string>? contextLinesBefore, 
        List<string>? contextLinesAfter, 
        CancellationToken cancellationToken)
    {
        await InitializeAsync();
        
        if (_translator == null)
        {
            throw new TranslationException("DeepL translator was not properly initialized.");
        }

        int retryCount = 0;
        var currentDelay = _retryDelay;

        while (true)
        {
            try
            {
                var result = await _translator.TranslateTextAsync(
                    text,
                    sourceLanguage,
                    targetLanguage,
                    cancellationToken: cancellationToken);

                return result.Text;
            }
            catch (DeepL.TooManyRequestsException tmrEx)
            {
                // Transient error; retry with exponential backoff
                retryCount++;
                _logger.LogWarning(tmrEx, "DeepL TooManyRequests; retry {RetryCount}/{MaxRetries} in {Delay}ms", retryCount, _maxRetries, currentDelay.TotalMilliseconds);
                if (retryCount > _maxRetries)
                {
                    _logger.LogError(tmrEx, "DeepL translation failed after retries");
                    throw new TranslationException("Translation using DeepL failed due to rate limiting.");
                }
                await Task.Delay(currentDelay, cancellationToken);
                currentDelay *= _retryDelayMultiplier;
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeepL translation failed");
                throw new TranslationException("Translation using DeepL failed.");
            }
        }
    }

    /// <summary>
    /// Retrieves the list of available target languages from DeepL, using caching to optimize performance.
    /// </summary>
    /// <param name="translator">The DeepL translator instance to use for the API call</param>
    /// <returns>An array of supported target languages, or null if the operation fails</returns>
    private async Task<DeepL.Model.TargetLanguage[]?> GetTargetLanguages(ITranslator translator)
    {
        if (Cache.TryGetValue(TargetLanguagesCacheKey, out DeepL.Model.TargetLanguage[]? targetLanguages))
        {
            return targetLanguages;
        }

        var languages = await translator.GetTargetLanguagesAsync();
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);
            
        Cache.Set(TargetLanguagesCacheKey, languages, cacheOptions);
        return languages;
    }
    
    /// <summary>
    /// Retrieves the list of available source languages from DeepL, using caching to optimize performance.
    /// </summary>
    /// <param name="translator">The DeepL translator instance to use for the API call</param>
    /// <returns>An array of supported source languages, or null if the operation fails</returns>
    private async Task<DeepL.Model.SourceLanguage[]?> GetSourceLanguages(ITranslator translator)
    {
        if (Cache.TryGetValue(SourceLanguagesCacheKey, out DeepL.Model.SourceLanguage[]? sourceLanguages))
        {
            return sourceLanguages;
        }

        var languages = await translator.GetSourceLanguagesAsync();
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);
            
        Cache.Set(SourceLanguagesCacheKey, languages, cacheOptions);
        return languages;
    }
    
    /// <inheritdoc />
    public override async Task<ModelsResponse> GetModels()
    {
        return await Task.FromResult(new ModelsResponse());
    }
}