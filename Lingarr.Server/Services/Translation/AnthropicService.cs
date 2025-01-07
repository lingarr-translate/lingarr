using System.Net;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class AnthropicService : BaseLanguageService
{
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _prompt;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AnthropicService(ISettingService settings,
        HttpClient httpClient,
        ILogger<AnthropicService> logger)
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
                SettingKeys.Translation.Anthropic.Model,
                SettingKeys.Translation.Anthropic.ApiKey,
                SettingKeys.Translation.Anthropic.Version,
                SettingKeys.Translation.AiPrompt
            ]);

            if (string.IsNullOrEmpty(settings[SettingKeys.Translation.Anthropic.Model]) ||
                string.IsNullOrEmpty(settings[SettingKeys.Translation.Anthropic.ApiKey]) ||
                string.IsNullOrEmpty(settings[SettingKeys.Translation.Anthropic.Version]))
            {
                throw new InvalidOperationException("Anthropic API key or model is not configured.");
            }

            _model = settings[SettingKeys.Translation.Anthropic.Model];
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings[SettingKeys.Translation.Anthropic.ApiKey]);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version",
                settings[SettingKeys.Translation.Anthropic.Version]);

            _prompt = !string.IsNullOrEmpty(settings[SettingKeys.Translation.Anthropic.ApiKey])
                ? settings[SettingKeys.Translation.Anthropic.ApiKey]
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
        string? text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);

        const int maxRetries = 5;
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(32);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = _model,
                    max_tokens = 1024,
                    messages = new[]
                    {
                        new { role = "system", content = _prompt },
                        new { role = "user", content = text }
                    }
                }), Encoding.UTF8, "application/json");

                var response =
                    await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, linked.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        throw new HttpRequestException("Rate limit exceeded", null, HttpStatusCode.TooManyRequests);
                    }

                    _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
                    _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync(cancellationToken: linked.Token));
                    throw new TranslationException("Translation using Anthropic failed.");
                }

                var responseBody = await response.Content.ReadAsStringAsync(linked.Token);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                return jsonResponse.GetProperty("content")[0].GetProperty("text").GetString() ??
                       throw new InvalidOperationException();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for text: {Text}", text);
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                _logger.LogWarning(
                    "Anthropic rate limit hit. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    delay, attempt, maxRetries);

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, maxDelay.Ticks));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Anthropic translation");
                throw new TranslationException("Failed to translate using Anthropic", ex);
            }
        }

        throw new TranslationException("Translation failed after maximum retry attempts.");
    }
}