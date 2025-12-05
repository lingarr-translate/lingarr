using System.Net;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Services.Translation.Base;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.Batch.Response;

namespace Lingarr.Server.Services.Translation;

public class AnthropicService : BaseLanguageService, ITranslationService, IBatchTranslationService
{
    private readonly string? _endpoint = "https://api.anthropic.com/v1";
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _prompt;
    private string? _apiKey;
    private string? _version;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

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
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.CustomAiParameters,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier
            ]);
            _model = settings[SettingKeys.Translation.Anthropic.Model];
            _apiKey = settings[SettingKeys.Translation.Anthropic.ApiKey];
            _version = settings[SettingKeys.Translation.Anthropic.Version];
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];

            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_version))
            {
                throw new InvalidOperationException("Anthropic API key, model or version is not configured.");
            }

            _replacements = new Dictionary<string, string>
            {
                ["sourceLanguage"] = GetFullLanguageName(sourceLanguage),
                ["targetLanguage"] = GetFullLanguageName(targetLanguage)
            };
            _prompt = ReplacePlaceholders(settings[SettingKeys.Translation.AiPrompt], _replacements);
            _contextPrompt = settings[SettingKeys.Translation.AiContextPrompt];
            _customParameters = PrepareCustomParameters(settings, SettingKeys.Translation.CustomAiParameters);
            
            var requestTimeout = int.TryParse(settings[SettingKeys.Translation.RequestTimeout],
                out var timeOut)
                ? timeOut
                : 5;
            _httpClient.Timeout = TimeSpan.FromMinutes(requestTimeout);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings[SettingKeys.Translation.Anthropic.ApiKey]);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version",
                settings[SettingKeys.Translation.Anthropic.Version]);

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
                var requestBody = new Dictionary<string, object>
                {
                    ["model"] = _model!,
                    ["system"] = _prompt!,
                    ["messages"] = new[]
                    {
                        new { role = "user", content = text }
                    }
                };
                
                requestBody = AddCustomParameters(requestBody);
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody), 
                    Encoding.UTF8, 
                    "application/json"
                );

                var response =
                    await _httpClient.PostAsync($"{_endpoint}/messages", content, linked.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        throw new HttpRequestException("Rate limit exceeded", null, HttpStatusCode.TooManyRequests);
                    }

                    _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
                    _logger.LogError("Response Content: {ResponseContent}",
                        await response.Content.ReadAsStringAsync(cancellationToken: linked.Token));
                    throw new TranslationException("Translation using Anthropic failed.");
                }

                var responseBody = await response.Content.ReadAsStringAsync(linked.Token);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var subtitleLine = jsonResponse.GetProperty("content")[0].GetProperty("text").GetString();
                return subtitleLine ?? throw new InvalidOperationException();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for text: {Text}", text);
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);
                
                _logger.LogWarning(
                    "Anthropic rate limit hit. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    delay, attempt, _maxRetries);
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

    /// <summary>
    /// Translates a batch of subtitles in a single API call using Anthropic's structured output
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
                return await TranslateBatchWithAnthropicApi(subtitleBatch, linked.Token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for batch translation");
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                _logger.LogWarning(
                    "Anthropic rate limit hit. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    delay, attempt, _maxRetries);

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during batch translation attempt {Attempt}", attempt);
                throw new TranslationException("Unexpected error occurred during batch translation.", ex);
            }
        }

        throw new TranslationException("Batch translation failed after maximum retry attempts.");
    }

    private async Task<Dictionary<int, string>> TranslateBatchWithAnthropicApi(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
    {
        // Check if we have any context-only items
        var hasContextItems = subtitleBatch.Any(item => item.IsContextOnly);
        var itemsToTranslate = subtitleBatch.Where(item => !item.IsContextOnly).ToList();
        
        // Build context-aware prompt if we have context items
        var effectivePrompt = _prompt!;
        if (hasContextItems)
        {
            effectivePrompt = _prompt + "\n\nIMPORTANT: Some items in the batch are marked with \"isContextOnly\": true. " +
                "These are provided ONLY for context to help you understand the conversation flow. " +
                "Do NOT translate or include context-only items in your output. " +
                "Only translate and return items where \"isContextOnly\" is false or not present.";
        }

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["max_tokens"] = 1024,
            ["system"] = effectivePrompt,
            ["tools"] = new[]
            {
                new
                {
                    name = "record_translation_batch",
                    description = "Record batch translation results using well-structured JSON.",
                    input_schema = new
                    {
                        type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["translations"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    ["position"] = new
                                    {
                                        type = "integer",
                                        description = "Position/index of the subtitle item"
                                    },
                                    ["line"] = new
                                    {
                                        type = "string",
                                        description = "Translated subtitle text"
                                    }
                                },
                                required = new[] { "position", "line" }
                            },
                            description = "Array of translated subtitle items with their positions"
                        }
                    },
                    required = new[] { "translations" }
                    }
                }
            },
            ["tool_choice"] = new
            {
                type = "tool",
                name = "record_translation_batch"
            },
            ["messages"] = new[]
            {
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(subtitleBatch)
                }
            }
        };

        // Add custom parameters but exclude tool-related ones
        if (_customParameters is { Count: > 0 })
        {
            foreach (var param in _customParameters)
            {
                if (param.Key != "tools" && param.Key != "tool_choice")
                {
                    requestBody[param.Key] = param.Value;
                }
            }
        }

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync($"{_endpoint}/messages", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Batch translation using Anthropic failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

        // Extract tool use result from Anthropic response
        if (!jsonResponse.TryGetProperty("content", out var contentArray) || 
            contentArray.GetArrayLength() == 0)
        {
            throw new TranslationException("Invalid response format from Anthropic API.");
        }

        JsonElement? toolUseContent = null;
        foreach (var contentItem in contentArray.EnumerateArray())
        {
            if (contentItem.TryGetProperty("type", out var typeProperty) && 
                typeProperty.GetString() == "tool_use")
            {
                toolUseContent = contentItem;
                break;
            }
        }

        if (!toolUseContent.HasValue || 
            !toolUseContent.Value.TryGetProperty("input", out var inputProperty) ||
            !inputProperty.TryGetProperty("translations", out var translationsProperty))
        {
            throw new TranslationException("Tool use result not found or invalid in Anthropic response.");
        }

        try
        {
            var translatedItems = new List<StructuredBatchResponse>();
            foreach (var translation in translationsProperty.EnumerateArray())
            {
                var position = translation.GetProperty("position").GetInt32();
                var line = translation.GetProperty("line").GetString() ?? string.Empty;

                translatedItems.Add(new StructuredBatchResponse
                {
                    Position = position,
                    Line = line
                });
            }

            // Only return translations for non-context items
            var expectedPositions = itemsToTranslate.Select(i => i.Position).ToHashSet();
            return translatedItems
                .Where(item => expectedPositions.Contains(item.Position))
                .GroupBy(item => item.Position)
                .ToDictionary(group => group.Key, group => group.First().Line);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse translated results from tool use");
            throw new TranslationException("Failed to parse translated subtitles", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<ModelsResponse> GetModels()
    {
        var settings = await _settings.GetSettings([
            SettingKeys.Translation.Anthropic.ApiKey,
            SettingKeys.Translation.Anthropic.Version
        ]);

        if (string.IsNullOrEmpty(settings[SettingKeys.Translation.Anthropic.ApiKey]))
        {
            return new ModelsResponse
            {
                Message = "Anthropic API key is not configured."
            };
        }

        if (string.IsNullOrEmpty(settings[SettingKeys.Translation.Anthropic.Version]))
        {
            return new ModelsResponse
            {
                Message = "Anthropic version is not configured."
            };
        }

        var url = $"{_endpoint}/models";

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", settings[SettingKeys.Translation.Anthropic.ApiKey]);
            request.Headers.Add("anthropic-version", settings[SettingKeys.Translation.Anthropic.Version]);

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

            if (!jsonResponse.TryGetProperty("data", out var dataElement))
            {
                return new ModelsResponse
                {
                    Message = "Invalid response format from Anthropic API."
                };
            }

            var labelValues = dataElement.EnumerateArray()
                .Select(model => new LabelValue
                {
                    // Set label to be name instead of display_name
                    Label = model.GetProperty("id").GetString() ?? string.Empty,
                    Value = model.GetProperty("id").GetString() ?? string.Empty
                })
                .ToList();

            return new ModelsResponse
            {
                Options = labelValues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from Anthropic API");
            return new ModelsResponse
            {
                Message = "Error fetching models from Anthropic API: " + ex.Message
            };
        }
    }
}