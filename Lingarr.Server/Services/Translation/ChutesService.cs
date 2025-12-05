using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Batch;
using Lingarr.Server.Models.Batch.Response;
using Lingarr.Server.Models.Integrations.Translation;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

/// <summary>
/// Chutes translation service using OpenAI-compatible API.
/// Get your API key at https://chutes.ai/app/api
/// </summary>
public class ChutesService : BaseLanguageService, ITranslationService, IBatchTranslationService
{
    private const string Endpoint = "https://llm.chutes.ai/v1";
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _prompt;
    private string? _apiKey;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

    public ChutesService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<ChutesService> logger)
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
                SettingKeys.Translation.Chutes.Model,
                SettingKeys.Translation.Chutes.ApiKey,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiBatchContextInstruction,
                SettingKeys.Translation.CustomAiParameters,
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier
            ]);
            _model = settings[SettingKeys.Translation.Chutes.Model];
            _apiKey = settings[SettingKeys.Translation.Chutes.ApiKey];
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];
            _batchContextInstruction = settings[SettingKeys.Translation.AiBatchContextInstruction];

            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Chutes API key or model is not configured.");
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
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

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

        return await TranslateWithChatApi(text, cancellationToken);
    }

    private async Task<string> TranslateWithChatApi(string? text, CancellationToken cancellationToken)
    {
        var messages = new[]
        {
            new { role = "system", content = _prompt },
            new { role = "user", content = text }
        };

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["messages"] = messages,
            ["stream"] = false
        };

        requestBody = AddCustomParameters(requestBody);

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync($"{Endpoint}/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Translation using Chutes API failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<DeepSeekChatResponse>(responseBody);

        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new TranslationException("Invalid or empty response from Chutes API.");
        }

        return chatResponse.Choices[0].Message.Content.Trim();
    }

    /// <inheritdoc />
    public override async Task<ModelsResponse> GetModels()
    {
        _apiKey = await _settings.GetSetting(
            SettingKeys.Translation.Chutes.ApiKey
        );

        if (string.IsNullOrEmpty(_apiKey))
        {
            return new ModelsResponse
            {
                Message = "Chutes API key is not configured. Get your key at https://chutes.ai/app/api"
            };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoint}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                    Message = "Invalid response format from Chutes API."
                };
            }

            var labelValues = dataElement.EnumerateArray()
                .Select(model => new LabelValue
                {
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
            _logger.LogError(ex, "Error fetching models from Chutes API");
            return new ModelsResponse
            {
                Message = "Error fetching models from Chutes API: " + ex.Message
            };
        }
    }

    /// <summary>
    /// Translates a batch of subtitles in a single API call using structured outputs
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
                return await TranslateBatchWithChutesApi(subtitleBatch, linked.Token);
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                _logger.LogWarning(ex,
                    "Chutes batch translation failed (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}...",
                    attempt, _maxRetries, delay);
                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Chutes batch translation attempt {Attempt}", attempt);
                throw new TranslationException("Unexpected error occurred during Chutes batch translation.", ex);
            }
        }

        throw new TranslationException("Batch translation failed after maximum retry attempts.");
    }

    private async Task<Dictionary<int, string>> TranslateBatchWithChutesApi(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
    {
        // 1. Context-only bookkeeping
        var hasContextItems = subtitleBatch.Any(item => item.IsContextOnly);
        var itemsToTranslate = subtitleBatch.Where(item => !item.IsContextOnly).ToList();

        // 2. Build effective prompt: only append batch instruction when both context AND feature are enabled
        var effectivePrompt = _prompt!;
        if (hasContextItems && _contextPromptEnabled == "true")
        {
            effectivePrompt = _prompt + "\n\n" + GetEffectiveBatchContextInstruction();
        }

        // 3. Ask Chutes to return structured JSON (same pattern as OpenAI)
        var responseFormat = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "batch_translation_response",
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        translations = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    position = new { type = "integer" },
                                    line = new { type = "string" }
                                },
                                required = new[] { "position", "line" },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[] { "translations" },
                    additionalProperties = false
                }
            }
        };

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["messages"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["role"] = "system",
                    ["content"] = effectivePrompt
                },
                new Dictionary<string, string>
                {
                    ["role"] = "user",
                    ["content"] = JsonSerializer.Serialize(subtitleBatch)
                }
            },
            ["response_format"] = responseFormat
        };

        // 4. Add custom parameters (but don't clobber response_format)
        if (_customParameters is { Count: > 0 })
        {
            foreach (var param in _customParameters)
            {
                if (param.Key != "response_format")
                {
                    requestBody[param.Key] = param.Value;
                }
            }
        }

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync($"{Endpoint}/chat/completions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Chutes batch response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Chutes batch response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Batch translation using Chutes API failed.");
        }

        var completionResponse =
            await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
        if (completionResponse?.Choices == null || completionResponse.Choices.Count == 0)
        {
            throw new TranslationException("No completion choices returned from Chutes batch API.");
        }

        var translatedJson = completionResponse.Choices[0].Message.Content;
        try
        {
            var responseWrapper = JsonSerializer.Deserialize<JsonElement>(translatedJson);
            if (!responseWrapper.TryGetProperty("translations", out var translationsElement))
            {
                throw new TranslationException("Response does not contain 'translations' property");
            }

            var translatedItems =
                JsonSerializer.Deserialize<List<StructuredBatchResponse>>(translationsElement.GetRawText());
            if (translatedItems == null)
            {
                throw new TranslationException("Failed to deserialize translated subtitles");
            }

            // Only map back lines for actual (non-context) items
            var expectedPositions = itemsToTranslate.Select(i => i.Position).ToHashSet();
            return translatedItems
                .Where(item => expectedPositions.Contains(item.Position))
                .GroupBy(item => item.Position)
                .ToDictionary(group => group.Key, group => group.First().Line);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse translated JSON from Chutes: {Json}", translatedJson);
            throw new TranslationException("Failed to parse translated subtitles", ex);
        }
    }
}
