﻿using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Integrations.Translation;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class LocalAiService : BaseLanguageService
{
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _endpoint;
    private string? _prompt;
    private List<KeyValuePair<string, object>>? _localAiParameters;
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

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.LocalAi.Model,
                SettingKeys.Translation.LocalAi.Endpoint,
                SettingKeys.Translation.LocalAi.ApiKey,
                SettingKeys.Translation.LocalAi.LocalAiParameters,
                SettingKeys.Translation.AiPrompt
            ]);

            if (string.IsNullOrEmpty(settings[SettingKeys.Translation.LocalAi.Model]) ||
                string.IsNullOrEmpty(settings[SettingKeys.Translation.LocalAi.Endpoint]))
            {
                throw new InvalidOperationException("Local AI address or model is not configured.");
            }

            _model = settings[SettingKeys.Translation.LocalAi.Model];
            _endpoint = settings[SettingKeys.Translation.LocalAi.Endpoint];
            _localAiParameters = PrepareLocalAiParameters(settings);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (settings.TryGetValue(SettingKeys.Translation.LocalAi.ApiKey, out var apiKey) &&
                !string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

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
    
    /// <summary>
    /// Prepares LocalAI parameters from settings for use in API requests.
    /// </summary>
    /// <param name="settings">Dictionary containing application settings.</param>
    private List<KeyValuePair<string, object>>? PrepareLocalAiParameters(Dictionary<string, string> settings)
    {
        if (!settings.TryGetValue(SettingKeys.Translation.LocalAi.LocalAiParameters, out var parametersJson) ||
            string.IsNullOrEmpty(parametersJson))
        {
            return null;
        }

        try
        {
            var parametersArray = JsonSerializer.Deserialize<JsonElement[]>(parametersJson);
            if (parametersArray == null)
            {
                return null;
            }

            var parameters = new List<KeyValuePair<string, object>>();
            foreach (var param in parametersArray)
            {
                if (!param.TryGetProperty("key", out var key) ||
                    !param.TryGetProperty("value", out var value)) continue;
                
                object valueObj = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString()!,
                    JsonValueKind.Number => value.TryGetInt64(out var intVal) ? intVal : value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => value.GetString()!
                };

                parameters.Add(new KeyValuePair<string, object>(key.GetString()!, valueObj));
            }
            return parameters;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LocalAiParameters: {Parameters}", parametersJson);
            return null;
        }
    }
    
    /// <summary>
    /// Adds Local AI parameters to the request data if they exist.
    /// </summary>
    /// <param name="requestData">The dictionary containing the base request parameters.</param>
    private Dictionary<string, object> AddLocalAiParameters(Dictionary<string, object> requestData)
    {
        if (_localAiParameters != null && _localAiParameters.Count > 0)
        {
            foreach (var param in _localAiParameters)
            {
                requestData[param.Key] = param.Value;
            }
        }
    
        return requestData;
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_prompt))
        {
            throw new InvalidOperationException("Local AI service was not properly initialized.");
        }
        
        using var retry = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, retry.Token);
        var isChatEndpoint = _endpoint.TrimEnd('/').EndsWith("completions", StringComparison.OrdinalIgnoreCase);
        
        const int maxRetries = 5;
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(32);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return isChatEndpoint 
                    ? await TranslateWithChatApi(text, retry.Token)
                    : await TranslateWithGenerateApi(text, retry.Token);
            }
            catch (TranslationResponseException ex)
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

    private async Task<string> TranslateWithGenerateApi(string text, CancellationToken cancellationToken)
    {
        var requestData = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["prompt"] = _prompt + "\n\n" + text,
            ["stream"] = false
        };
        requestData = AddLocalAiParameters(requestData);

        var content = new StringContent(JsonSerializer.Serialize(requestData), 
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync(cancellationToken));
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

        var requestData = new Dictionary<string, object>
        {
            ["model"] = _model!,
            ["messages"] = messages
        };
        requestData = AddLocalAiParameters(requestData);

        var content = new StringContent(JsonSerializer.Serialize(requestData),
            Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync(cancellationToken));
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
