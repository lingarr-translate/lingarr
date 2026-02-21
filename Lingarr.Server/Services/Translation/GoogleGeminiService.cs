using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations.Translation;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services.Translation.Base;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.Batch.Response;

namespace Lingarr.Server.Services.Translation;

public class GoogleGeminiService : BaseLanguageService, ITranslationService, IBatchTranslationService
{
    private readonly string? _endpoint = "https://generativelanguage.googleapis.com/v1beta";
    private readonly HttpClient _httpClient;
    private readonly IRequestTemplateService _requestTemplateService;
    private string? _model;
    private string? _apiKey;
    private string? _prompt;
    private string? _requestTemplate;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <inheritdoc />
    public override string? ModelName => _model;

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

    public GoogleGeminiService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<GoogleGeminiService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(settings, logger, languageCodeService, "/app/Statics/ai_languages.json")
    {
        _httpClient = httpClient;
        _requestTemplateService = requestTemplateService;
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
                SettingKeys.Translation.Gemini.RequestTemplate,
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier,
                SettingKeys.Translation.LanguageCodeFormat
            ]);
            _apiKey = settings[SettingKeys.Translation.Gemini.ApiKey];
            _model = settings[SettingKeys.Translation.Gemini.Model];
            _requestTemplate = !string.IsNullOrEmpty(settings[SettingKeys.Translation.Gemini.RequestTemplate])
                ? settings[SettingKeys.Translation.Gemini.RequestTemplate]
                : _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.Gemini.RequestTemplate);
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];

            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key or model is not configured.");
            }

            SetLanguageReplacements(sourceLanguage, targetLanguage, settings[SettingKeys.Translation.LanguageCodeFormat]);
            _prompt = ReplacePlaceholders(settings[SettingKeys.Translation.AiPrompt], _replacements);
            _contextPrompt = settings[SettingKeys.Translation.AiContextPrompt];

            var requestTimeout = int.TryParse(settings[SettingKeys.Translation.RequestTimeout],
                out var timeOut)
                ? timeOut
                : 5;
            _httpClient.Timeout = TimeSpan.FromMinutes(requestTimeout);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _maxRetries = int.TryParse(settings[SettingKeys.Translation.MaxRetries], out var maxRetries) 
                ? maxRetries 
                : 5;
            var retryDelaySeconds = int.TryParse(settings[SettingKeys.Translation.RetryDelay], out var delaySeconds) 
                ? delaySeconds 
                : 1;
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
        
        var delay = _retryDelay;
        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await TranslateWithGeminiApi(text, linked.Token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            {
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Max retries exhausted ({StatusCode}) for text: {Text}", ex.StatusCode, text);
                    throw new TranslationException($"Retry limit reached after {ex.StatusCode}.", ex);
                }

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);

                _logger.LogWarning(
                    "{ServiceName} received {StatusCode}. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    "Gemini", ex.StatusCode, delay, attempt, _maxRetries);
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
        var placeholders = new Dictionary<string, string>
        {
            ["model"] = _model!,
            ["systemPrompt"] = _prompt!,
            ["userMessage"] = message ?? string.Empty,
            ["sourceLanguage"] = _replacements["sourceLanguage"],
            ["targetLanguage"] = _replacements["targetLanguage"]
        };
        var bodyJson = _requestTemplateService.BuildRequestBody(_requestTemplate!, placeholders);

        var content = new StringContent(
            bodyJson,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini API request failed with status {StatusCode}: {ResponseContent}",
                response.StatusCode, 
                responseContent);
            throw new HttpRequestException(
                $"Gemini API request failed with status {response.StatusCode}: {responseContent}",
                null, 
                response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);

        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0 ||
            geminiResponse.Candidates[0].Content?.Parts == null ||
            geminiResponse.Candidates[0].Content?.Parts.Count == 0)
        {
            throw new TranslationException("Invalid or empty response from Gemini API.");
        }

        return geminiResponse.Candidates[0].Content?.Parts[0].Text ?? string.Empty;
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

    /// <summary>
    /// Translates a batch of subtitles in a single API call
    /// </summary>
    /// <param name="subtitleBatch">List of subtitles with position and content</param>
    /// <param name="sourceLanguage">Source language code</param>
    /// <param name="targetLanguage">Target language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping position to translated content</returns>
    public async Task<Dictionary<int, string>> TranslateBatchAsync(
        List<BatchSubtitleItem> subtitleBatch,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);
        
        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);
        
        var delay = _retryDelay;
        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await TranslateBatchWithGeminiApi(subtitleBatch, linked.Token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            {
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Max retries exhausted ({StatusCode}) for batch translation", ex.StatusCode);
                    throw new TranslationException($"Retry limit reached after {ex.StatusCode}.", ex);
                }

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);

                _logger.LogWarning(
                    "{ServiceName} received {StatusCode}. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    "Gemini", ex.StatusCode, delay, attempt, _maxRetries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during batch translation attempt {Attempt}", attempt);
                throw new TranslationException("Unexpected error occurred during batch translation.", ex);
            }
        }

        throw new TranslationException("Batch translation failed after maximum retry attempts.");
    }

    private async Task<Dictionary<int, string>> TranslateBatchWithGeminiApi(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
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
                            text = JsonSerializer.Serialize(subtitleBatch)
                        }
                    }
                }
            },
            ["generationConfig"] = new Dictionary<string, object>
            {
                ["response_mime_type"] = "application/json",
                ["response_schema"] = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            position = new
                            {
                                type = "integer"
                            },
                            line = new
                            {
                                type = "string"
                            }
                        },
                        required = new[] { "position", "line" }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini batch API request failed with status {StatusCode}: {ResponseContent}",
                response.StatusCode, 
                responseContent);
            throw new HttpRequestException(
                $"Gemini batch API request failed with status {response.StatusCode}: {responseContent}",
                null, 
                response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
        if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0 ||
            geminiResponse.Candidates[0].Content?.Parts == null ||
            geminiResponse.Candidates[0].Content?.Parts.Count == 0)
        {
            throw new TranslationException("Invalid or empty response from Gemini API.");
        }

        var translatedJson = geminiResponse.Candidates[0].Content?.Parts[0].Text ?? string.Empty;
        try
        {
            var translatedItems = JsonSerializer.Deserialize<List<StructuredBatchResponse>>(translatedJson);
            if (translatedItems == null)
            {
                throw new TranslationException("Failed to deserialize translated subtitles");
            }

            return translatedItems
                .GroupBy(item => item.Position)
                .ToDictionary(group => group.Key, group => group.First().Line);
        }

        catch (JsonException ex)
        {
            try
            {
                var repairedJson = TryRepairJson(translatedJson);
                if (repairedJson != translatedJson)
                {
                    var translatedItems = JsonSerializer.Deserialize<List<StructuredBatchResponse>>(repairedJson);
                    if (translatedItems != null)
                    {
                        _logger.LogWarning("Successfully repaired the truncated JSON response from Gemini. Please verify the result.");
                        return translatedItems
                            .GroupBy(item => item.Position)
                            .ToDictionary(group => group.Key, group => group.First().Line);
                    }
                }
            }
            catch
            {
                // Ignore repair failure
            }

            _logger.LogError(ex, "Failed to parse translated JSON: {Json}", translatedJson);
            throw new TranslationException("Failed to parse translated subtitles", ex);
        }
    }

    private static string TryRepairJson(string json)
    {
        json = json.Trim();
        if (json.StartsWith("[") && !json.EndsWith("]"))
        {
            var lastBrace = json.LastIndexOf('}');
            if (lastBrace > -1)
            {
                return json.Substring(0, lastBrace + 1) + "]";
            }
        }
        return json;
    }
}