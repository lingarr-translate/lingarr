using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations.Translation;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class GoogleGeminiService : BaseLanguageService
{
    private readonly string? _endpoint = "https://generativelanguage.googleapis.com/v1beta";
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
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.CustomAiParameters
            ]);
            _apiKey = settings[SettingKeys.Translation.Gemini.ApiKey];
            _model = settings[SettingKeys.Translation.Gemini.Model];
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];

            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key or model is not configured.");
            }

            _replacements = new Dictionary<string, string>
            {
                ["sourceLanguage"] = sourceLanguage,
                ["targetLanguage"] = targetLanguage
            };
            _prompt = ReplacePlaceholders(settings[SettingKeys.Translation.AiPrompt], _replacements);
            _contextPrompt = settings[SettingKeys.Translation.AiContextPrompt];
            _customParameters = PrepareCustomParameters(settings, SettingKeys.Translation.CustomAiParameters);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
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
        List<string>? contextLinesBefore, 
        List<string>? contextLinesAfter, 
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);
        
        text = ApplyContextIfEnabled(text, contextLinesBefore, contextLinesAfter);
        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);

        const int maxRetries = 5;
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(32);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await TranslateWithGeminiApi(text, linked.Token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for text: {Text}", text);
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
        var requestBody = new Dictionary<string, object>
        {
            ["systemInstruction"] = new 
            {
                parts = new[]
                {
                    new
                    {
                        text = _prompt
                    }
                }
            },
            ["contents"] = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = message
                        }
                    }
                }
            }
        };

        if (_customParameters is { Count: > 0 })
        {
            var generationConfig = new Dictionary<string, object>();
            foreach (var param in _customParameters)
            {
                generationConfig[param.Key] = param.Value;
            }
        
            requestBody["generationConfig"] = generationConfig;
        }

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
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
            geminiResponse.Candidates[0].Content?.Parts.Count == 0)
        {
            throw new TranslationException("Invalid or empty response from Gemini API.");
        }

        return geminiResponse.Candidates[0].Content?.Parts[0].Text ?? "";
    }

    /// <inheritdoc />
    public override async Task<ModelsResponse> GetModels()
    {
        var supportedGenerationMethods = new List<string> { "generateMessage", "generateContent", "generateText" };
        var apiKey = await _settings.GetSetting(
            SettingKeys.Translation.Gemini.ApiKey
        );

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ModelsResponse
            {
                Message = "Gemini API key is not configured."
            };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}/models?key={apiKey}");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch models. Status: {StatusCode}", response.StatusCode);
                return new ModelsResponse
                {
                    Message = $"Failed to fetch models. Status: {response.StatusCode}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (!jsonResponse.TryGetProperty("models", out var modelsElement))
            {
                return new ModelsResponse
                {
                    Message = "Invalid response format from Gemini API."
                };
            }

            var labelValues = modelsElement.EnumerateArray()
                .Where(model =>
                {
                    if (!model.TryGetProperty("supportedGenerationMethods", out var methods))
                        return false;

                    return methods.EnumerateArray()
                        .Any(method => supportedGenerationMethods.Contains(method.GetString()));
                })
                .Select(model => new LabelValue
                {
                    // Set label to be name instead of displayName
                    Label = model.GetProperty("name").GetString() ?? string.Empty,
                    Value = model.GetProperty("name").GetString()?.Replace("models/", "") ?? string.Empty
                })
                .ToList();

            return new ModelsResponse
            {
                Options = labelValues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from Gemini API");
            return new ModelsResponse
            {
                Message = "Error fetching models from Gemini API: " + ex.Message
            };
        }
    }
}