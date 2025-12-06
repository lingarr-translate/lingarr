using System.Net;
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

public class LocalAiService : BaseLanguageService, ITranslationService, IBatchTranslationService
{
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _endpoint;
    private string? _prompt;

    private bool _isChatEndpoint;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // retry settings
    private int _maxRetries;
    private TimeSpan _retryDelay;
    private int _retryDelayMultiplier;

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

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.LocalAi.Model,
                SettingKeys.Translation.LocalAi.Endpoint,
                SettingKeys.Translation.LocalAi.ApiKey,
                SettingKeys.Translation.AiPrompt,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.AiBatchContextInstruction,
                SettingKeys.Translation.CustomAiParameters,
                SettingKeys.Translation.RequestTimeout,
                SettingKeys.Translation.MaxRetries,
                SettingKeys.Translation.RetryDelay,
                SettingKeys.Translation.RetryDelayMultiplier
            ]);
            _model = settings[SettingKeys.Translation.LocalAi.Model];
            _endpoint = settings[SettingKeys.Translation.LocalAi.Endpoint];
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];
            _batchContextInstruction = settings[SettingKeys.Translation.AiBatchContextInstruction];

            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_endpoint))
            {
                throw new InvalidOperationException("Local AI service requires both endpoint address and model name to be configured in settings.");
            }

            _replacements = new Dictionary<string, string>
            {
                ["sourceLanguage"] = GetFullLanguageName(sourceLanguage),
                ["targetLanguage"] = GetFullLanguageName(targetLanguage)
            };
            _prompt = ReplacePlaceholders(settings[SettingKeys.Translation.AiPrompt], _replacements);
            _contextPrompt = settings[SettingKeys.Translation.AiContextPrompt];
            _customParameters = PrepareCustomParameters(settings, SettingKeys.Translation.CustomAiParameters);
            _isChatEndpoint = _endpoint.TrimEnd('/').EndsWith("completions", StringComparison.OrdinalIgnoreCase);

            var requestTimeout = int.TryParse(settings[SettingKeys.Translation.RequestTimeout],
                out var timeOut)
                ? timeOut
                : 5;
            _httpClient.Timeout = TimeSpan.FromMinutes(requestTimeout);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (settings.TryGetValue(SettingKeys.Translation.LocalAi.ApiKey, out var apiKey) &&
                !string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

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
    public override async Task<ModelsResponse> GetModels()
    {
        var settings = await _settings.GetSettings([
            SettingKeys.Translation.LocalAi.Endpoint,
            SettingKeys.Translation.LocalAi.ApiKey
        ]);
        
        var endpoint = settings[SettingKeys.Translation.LocalAi.Endpoint];
        var apiKey = settings[SettingKeys.Translation.LocalAi.ApiKey];

        if (string.IsNullOrEmpty(endpoint))
        {
            return new ModelsResponse
            {
                Message = "LocalAI endpoint is not configured."
            };
        }

        try
        {
            // Derive models endpoint from the configured endpoint
            // Common patterns:
            // .../v1/chat/completions -> .../v1/models
            // .../api/generate -> .../api/tags (Ollama) or .../models
            
            var modelsEndpoint = endpoint.TrimEnd('/');
            if (modelsEndpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                modelsEndpoint = modelsEndpoint.Substring(0, modelsEndpoint.Length - "/chat/completions".Length) + "/models";
            }
            else if (modelsEndpoint.EndsWith("/generate", StringComparison.OrdinalIgnoreCase))
            {
                 // Assuming standard LocalAI behaviour or OpenAI compatible /v1/models
                 modelsEndpoint = modelsEndpoint.Substring(0, modelsEndpoint.Length - "/generate".Length) + "/models";
            }
            else if (!modelsEndpoint.EndsWith("/models", StringComparison.OrdinalIgnoreCase))
            {
                 // Fallback append
                 modelsEndpoint += "/models";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, modelsEndpoint);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var labelValues = new List<LabelValue>();
            
            // Handle standard OpenAI format: { "data": [ { "id": "..." } ] }
            if (root.TryGetProperty("data", out var data))
            {
                foreach (var model in data.EnumerateArray())
                {
                    if (model.TryGetProperty("id", out var idElement))
                    {
                        var id = idElement.GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            labelValues.Add(new LabelValue { Label = id, Value = id });
                        }
                    }
                }
            }
            // Handle Ollama format: { "models": [ { "name": "..." } ] } (if hitting /api/tags)
            else if (root.TryGetProperty("models", out var models))
            {
                 foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var nameElement))
                    {
                        var name = nameElement.GetString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            labelValues.Add(new LabelValue { Label = name, Value = name });
                        }
                    }
                }
            }
            
            if (labelValues.Count == 0)
            {
                 return new ModelsResponse { Message = "No models found in response." };
            }

            return new ModelsResponse
            {
                Options = labelValues.OrderBy(x => x.Label).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from LocalAI");
            return new ModelsResponse
            {
                Message = "Error fetching models from LocalAI: " + ex.Message
            };
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
                return _isChatEndpoint
                    ? await TranslateWithChatApi(text, retry.Token)
                    : await TranslateWithGenerateApi(text, retry.Token);
            }
            catch (TranslationResponseException ex)
            {
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for text: {Text}", text);
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                await Task.Delay(delay, linked.Token).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * _retryDelayMultiplier);

                _logger.LogWarning(
                    "429 Too Many Requests. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
                    delay, attempt, _maxRetries);
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

    /// <summary>
    /// Translates a batch of subtitles in a single API call using structured outputs fallback
    /// Since LocalAI may not support structured outputs, we'll attempt structured format first,
    /// then fall back to regular parsing if needed
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
                return await TranslateBatchWithLocalAiApi(subtitleBatch, linked.Token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == _maxRetries)
                {
                    _logger.LogError(ex, "Too many requests. Max retries exhausted for batch translation");
                    throw new TranslationException("Too many requests. Retry limit reached.", ex);
                }

                _logger.LogWarning(
                    "429 Too Many Requests. Retrying in {Delay}... (Attempt {Attempt}/{MaxRetries})",
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

    private async Task<Dictionary<int, string>> TranslateBatchWithLocalAiApi(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
    {
        if (!_isChatEndpoint)
        {
            return await TranslateBatchWithGenerateApi(subtitleBatch, cancellationToken);
        }

        // Try structured output first (OpenAI-compatible format)
        try
        {
            return await TranslateBatchWithStructuredOutput(subtitleBatch, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Structured output failed, falling back to JSON parsing");
            return await TranslateBatchWithJsonParsing(subtitleBatch, cancellationToken);
        }
    }

    private async Task<Dictionary<int, string>> TranslateBatchWithStructuredOutput(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
    {
        // Check if we have any context-only items
        var hasContextItems = subtitleBatch.Any(item => item.IsContextOnly);
        var itemsToTranslate = subtitleBatch.Where(item => !item.IsContextOnly).ToList();
        
        // Build context-aware prompt if we have context items and context prompting is enabled
        var effectivePrompt = _prompt!;
        if (hasContextItems && _contextPromptEnabled == "true")
        {
            effectivePrompt = _prompt + "\n\n" + GetEffectiveBatchContextInstruction();
        }

        var responseFormat = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "batch_translation_response",
                strict = true,
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
                                    position = new
                                    {
                                        type = "integer",
                                        description = "Position number of the subtitle item"
                                    },
                                    line = new
                                    {
                                        type = "string",
                                        description = "Translated subtitle text"
                                    }
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

        var messages = new[]
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
        };

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["messages"] = messages,
            ["response_format"] = responseFormat
        };

        // Add custom parameters but exclude response_format to avoid conflicts
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

        var requestContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(_endpoint, requestContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Batch translation using LocalAI structured output failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseBody);
        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new TranslationException("No completion choices returned from LocalAI");
        }

        var translatedJson = chatResponse.Choices[0].Message.Content;

        try
        {
            // Parse the wrapper object first, extract the translations array
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

            // Only return translations for non-context items
            var expectedPositions = itemsToTranslate.Select(i => i.Position).ToHashSet();
            return translatedItems
                .Where(item => expectedPositions.Contains(item.Position))
                .GroupBy(item => item.Position)
                .ToDictionary(group => group.Key, group => group.First().Line);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse structured JSON response: {Json}", translatedJson);
            throw new TranslationException("Failed to parse structured translated subtitles", ex);
        }
    }

    private async Task<Dictionary<int, string>> TranslateBatchWithJsonParsing(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
    {
        // Check if we have any context-only items
        var hasContextItems = subtitleBatch.Any(item => item.IsContextOnly);
        var itemsToTranslate = subtitleBatch.Where(item => !item.IsContextOnly).ToList();
        
        // Build context-aware prompt if we have context items and context prompting is enabled
        var effectivePrompt = _prompt!;
        if (hasContextItems && _contextPromptEnabled == "true")
        {
            effectivePrompt = _prompt + "\n\n" + GetEffectiveBatchContextInstruction();
        }

        var messages = new[]
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
        };

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["messages"] = messages
        };

        requestBody = AddCustomParameters(requestBody);
        var requestContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(_endpoint, requestContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Batch translation using LocalAI JSON parsing failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseBody);

        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new TranslationException("No completion choices returned from LocalAI");
        }

        // Try to extract JSON
        var translatedJson = chatResponse.Choices[0].Message.Content;
        var jsonStart = translatedJson.IndexOf('[');
        var jsonEnd = translatedJson.LastIndexOf(']');
        if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
        {
            translatedJson = translatedJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        try
        {
            var translatedItems = JsonSerializer.Deserialize<List<StructuredBatchResponse>>(translatedJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (translatedItems == null)
            {
                throw new TranslationException("Failed to deserialize translated subtitles from JSON parsing");
            }

            // Only return translations for non-context items
            var expectedPositions = itemsToTranslate.Select(i => i.Position).ToHashSet();
            return translatedItems
                .Where(item => expectedPositions.Contains(item.Position))
                .GroupBy(item => item.Position)
                .ToDictionary(group => group.Key, group => group.First().Line);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON response: {Json}", translatedJson);
            throw new TranslationException("Failed to parse JSON translated subtitles", ex);
        }
    }

    private async Task<Dictionary<int, string>> TranslateBatchWithGenerateApi(
        List<BatchSubtitleItem> subtitleBatch,
        CancellationToken cancellationToken)
    {
        // Check if we have any context-only items
        var hasContextItems = subtitleBatch.Any(item => item.IsContextOnly);
        var itemsToTranslate = subtitleBatch.Where(item => !item.IsContextOnly).ToList();
        
        // Build context-aware prompt if we have context items and context prompting is enabled
        var effectivePrompt = _prompt;
        if (hasContextItems && _contextPromptEnabled == "true")
        {
            effectivePrompt = _prompt + "\n\n" + GetEffectiveBatchContextInstruction();
        }

        var batchPrompt = effectivePrompt +
                          "\n\nPlease return the response as a JSON array with objects containing 'position' and 'line' fields. Example: [{\"position\": 1, \"line\": \"translated text\"}]\n\n";

        var requestData = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["prompt"] = batchPrompt + JsonSerializer.Serialize(subtitleBatch),
            ["stream"] = false
        };
        requestData = AddCustomParameters(requestData);

        var content = new StringContent(JsonSerializer.Serialize(requestData),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Batch translation using Local AI generate API failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var generateResponse = JsonSerializer.Deserialize<GenerateResponse>(responseBody);

        if (generateResponse == null || string.IsNullOrEmpty(generateResponse.Response))
        {
            throw new TranslationException("Invalid or empty response from generate API.");
        }

        var translatedJson = generateResponse.Response;

        // Try to extract JSON from the response
        var jsonStart = translatedJson.IndexOf('[');
        var jsonEnd = translatedJson.LastIndexOf(']');

        if (jsonStart != -1 && jsonEnd != -1 && jsonEnd > jsonStart)
        {
            translatedJson = translatedJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        try
        {
            var translatedItems = JsonSerializer.Deserialize<List<StructuredBatchResponse>>(translatedJson);

            if (translatedItems == null)
            {
                throw new TranslationException("Failed to deserialize translated subtitles from generate API");
            }

            // Only return translations for non-context items
            var expectedPositions = itemsToTranslate.Select(i => i.Position).ToHashSet();
            return translatedItems
                .Where(item => expectedPositions.Contains(item.Position))
                .GroupBy(item => item.Position)
                .ToDictionary(group => group.Key, group => group.First().Line);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse generate API JSON response: {Json}", translatedJson);
            throw new TranslationException("Failed to parse generate API translated subtitles", ex);
        }
    }

    private async Task<string> TranslateWithGenerateApi(string text, CancellationToken cancellationToken)
    {
        var requestData = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["prompt"] = _prompt + "\n\n" + text,
            ["stream"] = false
        };
        requestData = AddCustomParameters(requestData);

        var content = new StringContent(JsonSerializer.Serialize(requestData),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Translation using Local AI failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var generateResponse = JsonSerializer.Deserialize<GenerateResponse>(responseBody);

        if (generateResponse == null || string.IsNullOrEmpty(generateResponse.Response))
        {
            throw new TranslationException("Invalid or empty response from generate API.");
        }

        return generateResponse.Response;
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
            ["messages"] = messages
        };
        requestBody = AddCustomParameters(requestBody);

        var content = new StringContent(JsonSerializer.Serialize(requestBody),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationResponseException("Translation using chat API failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseBody);

        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new TranslationResponseException("Invalid or empty response from chat API.");
        }

        return chatResponse.Choices[0].Message.Content;
    }
}
