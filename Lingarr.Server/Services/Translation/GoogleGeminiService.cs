using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Integrations.Translation;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class GoogleGeminiService : BaseLanguageService
{
    private string? _endpoint = "https://generativelanguage.googleapis.com/v1beta";
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _apiKey;
    private string? _prompt;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public GoogleGeminiService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<GoogleGeminiService> logger)
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

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.Gemini.Model,
                SettingKeys.Translation.Gemini.ApiKey,
                SettingKeys.Translation.AiPrompt
            ]);

            if (string.IsNullOrEmpty(settings[SettingKeys.Translation.Gemini.ApiKey]))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            _apiKey = settings[SettingKeys.Translation.Gemini.ApiKey];
            _model = settings[SettingKeys.Translation.Gemini.Model];

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _prompt = !string.IsNullOrEmpty(settings[SettingKeys.Translation.AiPrompt])
                ? settings[SettingKeys.Translation.AiPrompt]
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
    public override async Task<string> TranslateAsync(
        string message,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_model))
        {
            throw new InvalidOperationException("Gemini service was not properly initialized.");
        }

        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);

        const int maxRetries = 5;
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(32);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await TranslateWithGeminiApi(message, linked.Token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for text: {Text}", message);
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                _logger.LogWarning(
                    "429 Too Many Requests. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    delay, attempt, maxRetries);

                await Task.Delay(delay, linked.Token);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, maxDelay.Ticks));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during translation attempt {Attempt}", attempt);
                throw new TranslationException("Unexpected error occurred during translation.", ex);
            }
        }

        throw new TranslationException("Translation failed after maximum retry attempts.");
    }

    private async Task<string> TranslateWithGeminiApi(string? message, CancellationToken cancellationToken)
    {
        var endpoint = $"{_endpoint}/models/{_model}:generateContent?key={_apiKey}";

        var request = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = _prompt
                        },
                        new
                        {
                            text = message
                        }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", 
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Translation using Gemini API failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
        
        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0 ||
            geminiResponse.Candidates[0].Content?.Parts == null || 
            geminiResponse.Candidates[0]?.Content?.Parts.Count == 0)
        {
            throw new TranslationException("Invalid or empty response from Gemini API.");
        }

        return geminiResponse?.Candidates[0]?.Content?.Parts[0].Text ?? "";
    }
}