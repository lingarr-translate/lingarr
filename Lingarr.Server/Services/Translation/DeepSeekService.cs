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
    private readonly IRequestTemplateService _requestTemplateService;
    private string? _model;
    private string? _prompt;
    private string? _apiKey;
    private string? _requestTemplate;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <inheritdoc />
    public override string? ModelName => _model;

    public DeepSeekService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<DeepSeekService> logger,
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
                SettingKeys.Translation.DeepSeek.Model,
                SettingKeys.Translation.DeepSeek.ApiKey,
                SettingKeys.Translation.DeepSeek.RequestTemplate,
                SettingKeys.Translation.AiContextPromptEnabled,
                SettingKeys.Translation.AiContextPrompt,
                SettingKeys.Translation.AiPrompt
            ]);
            _model = settings[SettingKeys.Translation.DeepSeek.Model];
            _apiKey = settings[SettingKeys.Translation.DeepSeek.ApiKey];
            _requestTemplate = !string.IsNullOrEmpty(settings[SettingKeys.Translation.DeepSeek.RequestTemplate])
                ? settings[SettingKeys.Translation.DeepSeek.RequestTemplate]
                : _requestTemplateService.GetDefaultTemplate(SettingKeys.Translation.DeepSeek.RequestTemplate);
            _contextPromptEnabled = settings[SettingKeys.Translation.AiContextPromptEnabled];

            if (string.IsNullOrEmpty(_model) || string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("DeepSeek API key or model is not configured.");
            }

            _replacements = new Dictionary<string, string>
            {
                ["sourceLanguage"] = GetFullLanguageName(sourceLanguage),
                ["targetLanguage"] = GetFullLanguageName(targetLanguage)
            };
            _prompt = ReplacePlaceholders(settings[SettingKeys.Translation.AiPrompt], _replacements);
            _contextPrompt = settings[SettingKeys.Translation.AiContextPrompt];
            
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

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
        var placeholders = new Dictionary<string, string>
        {
            ["model"] = _model!,
            ["systemPrompt"] = _prompt!,
            ["userMessage"] = text ?? string.Empty
        };
        var bodyJson = _requestTemplateService.BuildRequestBody(_requestTemplate!, placeholders);

        var content = new StringContent(
            bodyJson,
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync($"{_endpoint}/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "DeepSeek API request failed with status {StatusCode}: {ResponseContent}",
                response.StatusCode, 
                responseContent);
            throw new TranslationException(
                $"DeepSeek API request failed with status {response.StatusCode}: {responseContent}");
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
        _apiKey = await _settings.GetSetting(
            SettingKeys.Translation.DeepSeek.ApiKey
        );

        if (string.IsNullOrEmpty(_apiKey))
        {
            return new ModelsResponse
            {
                Message = "DeepSeek API key is not configured."
            };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_endpoint}/models");
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