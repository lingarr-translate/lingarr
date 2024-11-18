using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public LibreService(
        HttpClient httpClient,
        ISettingService settings,
        ILogger<LibreService> logger) 
        : base(settings, logger, "/app/Statics/libre_translate_languages.json")
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

            _apiUrl = await _settings.GetSetting("libretranslate_url") ?? "http://libretranslate:5000";
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage, 
        CancellationToken cancellationToken)
    {
        await InitializeAsync();

        if (string.IsNullOrEmpty(_apiUrl))
        {
            throw new InvalidOperationException("LibreTranslate URL is not configured.");
        }

        var content = new StringContent(JsonSerializer.Serialize(new
        {
            q = text,
            source = sourceLanguage,
            target = targetLanguage,
            format = "text"
        }), Encoding.UTF8, "application/json");
        
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        var response = await _httpClient.PostAsync($"{_apiUrl}/translate", content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync());
            throw new TranslationException("Translation using LibreTranslate failed.");
        }
        
        var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
        return result?.TranslatedText ?? string.Empty;
    }
}