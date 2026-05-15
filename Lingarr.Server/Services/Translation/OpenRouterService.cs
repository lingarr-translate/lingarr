using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Core.Configuration;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

/// <summary>
/// OpenRouter translation service.
/// Provides access to 100+ models through a unified OpenAI-compatible API.
/// </summary>
public class OpenRouterService : BaseLanguageService
{
    private string? _endpoint = "https://openrouter.ai/api/v1/";
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

    public OpenRouterService(
        ISettingService settings,
        HttpClient httpClient,
        ILogger<OpenRouterService> logger,
        LanguageCodeService languageCodeService,
        IRequestTemplateService requestTemplateService)
        : base(settings, logger, languageCodeService, "/app/Statics/ai_languages.json")
    {
        _httpClient = httpClient;
        _requestTemplateService = requestTemplateService;
    }

    private async Task InitializeAsync(string sourceLanguage, string targetLanguage)
    {
        if (_initialized) return;

        try
        {
            await _initLock.WaitAsync();
            if (_initialized) return;

            var settings = await _settings.GetSettings([
                SettingKeys.Translation.OpenRouter.Endpoint,
                SettingKeys.Translation.OpenRouter.Model,
                SettingKeys.Translation.OpenRouter.RequestTemplate,
                SettingKeys.Translation.AiPrompt
            ]);

            _endpoint = settings[SettingKeys.Translation.OpenRouter.Endpoint];
            if (string.IsNullOrWhiteSpace(_endpoint))
            {
                _endpoint = "https://openrouter.ai/api/v1/";
            }
            if (!_endpoint.EndsWith("/"))
            {
                _endpoint += "/";
            }

            _model = settings[SettingKeys.Translation.OpenRouter.Model];
            _requestTemplate = settings[SettingKeys.Translation.OpenRouter.RequestTemplate];
            _prompt = settings[SettingKeys.Translation.AiPrompt];

            _apiKey = await _settings.GetEncryptedSetting(SettingKeys.Translation.OpenRouter.ApiKey);

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("OpenRouter API key is not configured.");
            }

            if (string.IsNullOrEmpty(_model))
            {
                throw new InvalidOperationException("OpenRouter model is not selected.");
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        List<string>? contextLinesBefore,
        List<string>? contextLinesAfter,
        CancellationToken cancellationToken)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);

        var systemPrompt = _prompt?
            .Replace("{sourceLanguage}", sourceLanguage)
            .Replace("{targetLanguage}", targetLanguage) ??
            $"Translate from {sourceLanguage} to {targetLanguage}. Only return the translated text without any additional explanation.";

        var userContent = new StringBuilder();

        if (contextLinesBefore?.Any() == true)
        {
            userContent.AppendLine("Previous context:");
            contextLinesBefore.ForEach(line => userContent.AppendLine(line));
            userContent.AppendLine();
        }

        userContent.AppendLine("Translate this text:");
        userContent.AppendLine(text);

        if (contextLinesAfter?.Any() == true)
        {
            userContent.AppendLine();
            userContent.AppendLine("Following context:");
            contextLinesAfter.ForEach(line => userContent.AppendLine(line));
        }

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userContent.ToString() }
        };

        var requestBody = new
        {
            model = _model,
            messages,
            temperature = 0.3,
            max_tokens = 4096
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}chat/completions")
        {
            Content = content
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Add("HTTP-Referer", "https://github.com/lingarr-translate/lingarr");
        request.Headers.Add("X-Title", "Lingarr");

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new TranslationException($"OpenRouter API error ({response.StatusCode}): {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var translatedText = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return translatedText?.Trim() ?? string.Empty;
    }

    public async Task<List<SourceLanguage>> GetLanguages()
    {
        return await Task.FromResult(new List<SourceLanguage>());
    }

    public override async Task<ModelsResponse> GetModels()
    {
        _apiKey ??= await _settings.GetEncryptedSetting(SettingKeys.Translation.OpenRouter.ApiKey);

        try
        {
            var client = new HttpClient();
            if (!string.IsNullOrEmpty(_apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/lingarr-translate/lingarr");
            client.DefaultRequestHeaders.Add("X-Title", "Lingarr");

            var response = await client.GetAsync("https://openrouter.ai/api/v1/models");

            if (!response.IsSuccessStatusCode)
            {
                return new ModelsResponse { Message = $"Failed to fetch models: {response.StatusCode}" };
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var dataElement))
            {
                return new ModelsResponse { Message = "No models data returned." };
            }

            var options = dataElement.EnumerateArray()
                .Select(model =>
                {
                    var id = model.GetProperty("id").GetString() ?? "";
                    var name = model.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;

                    var label = name;
                    if (model.TryGetProperty("context_length", out var ctx) && ctx.ValueKind == JsonValueKind.Number)
                    {
                        label += $" • {ctx.GetInt32() / 1000}k ctx";
                    }

                    return new LabelValue { Label = label, Value = id };
                })
                .OrderBy(x => x.Label)
                .ToList();

            return new ModelsResponse { Options = options };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching OpenRouter models");
            return new ModelsResponse { Message = ex.Message };
        }
    }
}
