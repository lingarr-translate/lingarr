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

public class DeepSeekService : BaseLanguageService
{
    private string? _endpoint = "https://api.deepseek.com";
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _prompt;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DeepSeekService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<DeepSeekService> logger)
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
                SettingKeys.Translation.DeepSeek.Model,
                SettingKeys.Translation.DeepSeek.ApiKey,
                SettingKeys.Translation.AiPrompt
            ]);

            if (string.IsNullOrEmpty(settings[SettingKeys.Translation.DeepSeek.Model]))
            {
                throw new InvalidOperationException("DeepSeek model is not configured.");
            }

            var apiKey = settings[SettingKeys.Translation.DeepSeek.ApiKey];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("DeepSeek API key is not configured.");
            }

            _model = settings[SettingKeys.Translation.DeepSeek.Model];

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_prompt))
        {
            throw new InvalidOperationException("DeepSeek service was not properly initialized.");
        }

        return await TranslateWithChatApi(text, cancellationToken);
    }

    private async Task<string> TranslateWithChatApi(string? text, CancellationToken cancellationToken)
    {
        var messages = new[]
        {
            new { role = "system", content = _prompt },
            new { role = "user", content = text }
        };

        var requestBody = new
        {
            model = _model,
            messages,
            stream = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync($"{_endpoint}/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}",
                await response.Content.ReadAsStringAsync(cancellationToken));
            throw new TranslationException("Translation using DeepSeek API failed.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<DeepSeekChatResponse>(responseBody);

        if (chatResponse?.Choices == null || chatResponse.Choices.Count == 0)
        {
            throw new TranslationException("Invalid or empty response from DeepSeek API.");
        }

        return chatResponse.Choices[0].Message.Content.Trim();
    }

    /// <inheritdoc />
    public override async Task<ModelsResponse> GetModels()
    {
        var apiKey = await _settings.GetSetting(
            SettingKeys.Translation.DeepSeek.ApiKey
        );

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ModelsResponse
            {
                Message = "DeepSeek API key is not configured."
            };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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
                    Message = "Invalid response format from DeepSeek API."
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
            _logger.LogError(ex, "Error fetching models from DeepSeek API");
            return new ModelsResponse
            {
                Message = "Error fetching models from DeepSeek API: " + ex.Message
            };
        }
    }
}