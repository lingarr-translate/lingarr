using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class LibreService : BaseLanguageService
{
    private readonly HttpClient _httpClient;
    private string? _apiUrl;
    private string? _apiKey;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

    /// <inheritdoc />
    public override string? ModelName => null;

    public LibreService(
        HttpClient httpClient,
        ISettingService settings,
        ILogger<LibreService> logger,
        LanguageCodeService languageCodeService)
        : base(settings, logger, languageCodeService, "/app/Statics/libre_translate_languages.json")
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Initializes the translation service with necessary configurations and credentials.
    /// This method is thread-safe and ensures one-time initialization of service dependencies.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation</returns>
    private async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            await _initLock.WaitAsync();
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.LibreTranslate.Url,
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier
            ]);
            _apiUrl = settings[SettingKeys.Translation.LibreTranslate.Url];
            _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.LibreTranslate.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
        _logger.LogInformation($"Retrieving |Green|LibreTranslate|/Green| languages");
        await InitializeAsync();

        var libreTranslateLanguagesUrl = "https://libretranslate.com/languages";
        List<JsonLanguage>? libreLanguages;
        try
        {
            libreLanguages = await _httpClient.GetFromJsonAsync<List<JsonLanguage>>(libreTranslateLanguagesUrl);
            if (libreLanguages == null || !libreLanguages.Any())
            {
                _logger.LogWarning("Received no languages from LibreTranslate API at {Url}. The endpoint might be down or returned an empty list.", libreTranslateLanguagesUrl);
                return [];
            }

            _logger.LogInformation("Successfully retrieved {Count} languages from LibreTranslate API.", libreLanguages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving or processing LibreTranslate languages.");
            return [];
        }

        var languageCodes = libreLanguages.Select(l => l.Code).ToHashSet();
        return libreLanguages
            .Select(lang => new SourceLanguage
            {
                Code = lang.Code,
                Name = lang.Name,
                Targets = languageCodes
                    .Where(code => code != lang.Code)
                    .ToList()
            })
            .ToList();
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

        if (string.IsNullOrEmpty(_apiUrl))
        {
            throw new InvalidOperationException("LibreTranslate URL is not configured.");
        }

        var delay = _retryDelay;
        // Run for 1 initial attempt + _maxRetries
        for (var attempt = 1; attempt <= _maxRetries + 1; attempt++)
        {
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                q = text,
                source = sourceLanguage,
                target = targetLanguage,
                format = "text",
                api_key = _apiKey
            }), Encoding.UTF8, "application/json");

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/translate", content, cancellationToken);

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            {
                if (attempt > _maxRetries)
                {
                    _logger.LogError("Max retries exhausted ({StatusCode}) for text: {Text}", response.StatusCode, text);
                    throw new TranslationException(
                        $"LibreTranslate translation failed with status {response.StatusCode} ({response.ReasonPhrase}): retry limit reached.");
                }

                _logger.LogWarning(
                    "LibreTranslate received {StatusCode}. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    response.StatusCode, delay, attempt, _maxRetries);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
                _logger.LogError("Response Content: {ResponseContent}", responseBody);
                throw new TranslationException(
                    $"LibreTranslate translation failed with status {response.StatusCode} ({response.ReasonPhrase}): {responseBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
            return result?.TranslatedText ?? string.Empty;
        }

        throw new TranslationException("Translation failed after maximum retry attempts.");
    }
}
