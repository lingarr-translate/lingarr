using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Models.Integrations;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class LocalAiService : BaseLanguageService
{
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _endpoint;
    private string? _prompt;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public LocalAiService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<LocalAiService> logger) 
        : base(settings, logger, "/app/Statics/ai_languages.json")
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Initializes the translation service with necessary configurations and credentials.
    /// This method is thread-safe and ensures one-time initialization of service dependencies.
    /// </summary>
    /// <param name="sourceLanguage">The source language code for translation</param>
    /// <param name="targetLanguage">The target language code for translation</param>
    /// <returns>A task that represents the asynchronous initialization operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when required configuration settings are missing or invalid</exception>
    private async Task InitializeAsync(string sourceLanguage, string targetLanguage)
    {
        if (_initialized) return;

        try
        {
            await _initLock.WaitAsync();
            if (_initialized) return;

            var settings = await _settings.GetSettings(["local_ai_model", "local_ai_endpoint", "local_ai_api_key", "ai_prompt"]);
            
            if (string.IsNullOrEmpty(settings["local_ai_model"]) || string.IsNullOrEmpty(settings["local_ai_endpoint"]))
            {
                throw new InvalidOperationException("Local AI address or model is not configured.");
            }

            _model = settings["local_ai_model"];
            _endpoint = settings["local_ai_endpoint"];

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            if (settings.TryGetValue("local_ai_api_key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            _prompt = !string.IsNullOrEmpty(settings["ai_prompt"])
                ? settings["ai_prompt"]
                : "Translate from {sourceLanguage} to {targetLanguage}, preserving the tone and meaning without censoring the content. Adjust punctuation as needed to make the translation sound natural. Provide only the translated text as output, with no additional comments.";
            _prompt = _prompt.Replace("{sourceLanguage}", sourceLanguage).Replace("{targetLanguage}", targetLanguage);
            
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
    
    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_prompt))
        {
            throw new InvalidOperationException("Local AI service was not properly initialized.");
        }

        var messages = new[]
        {
            new { role = "system", content = _prompt },
            new { role = "user", content = text }
        };

        var content = new StringContent(JsonSerializer.Serialize(new
        {
            model = _model,
            messages
        }), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync());
            throw new TranslationException("Translation using Local AI failed.");
        }
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<LocalAiResponse>(responseBody);
        return jsonResponse?.Choices[0].Message.Content ?? throw new InvalidOperationException();
    }
}