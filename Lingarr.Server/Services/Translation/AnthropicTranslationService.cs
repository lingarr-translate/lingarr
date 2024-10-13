using System.Text;
using System.Text.Json;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Services.Translation;

public class AnthropicTranslationService : TranslationServiceBase
{
    private readonly HttpClient _httpClient;

    public AnthropicTranslationService(ISettingService settings,
        HttpClient httpClient,
        ILogger<AnthropicTranslationService> logger) : base(settings, logger)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var settings = await _settings.GetSettings(["anthropic_model", "anthropic_api_key", "anthropic_version"]);
        if (string.IsNullOrEmpty(settings["anthropic_model"]) || string.IsNullOrEmpty(settings["anthropic_api_key"]) || string.IsNullOrEmpty(settings["anthropic_version"]))
        {
            throw new InvalidOperationException("Anthropic API key or model is not configured.");
        }
        
        var prompt = $"You will be provided with a sentence in {sourceLanguage} and your task is to translate it into {targetLanguage}";
        _httpClient.DefaultRequestHeaders.Add("x-api-key", settings["anthropic_api_key"]);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", settings["anthropic_version"]);
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings["anthropic_model"],
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "system", content = prompt },
                new { role = "user", content = text }
            }
        }), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync());
            throw new TranslationException("Translation using Anthropic failed.");
        }
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        return jsonResponse.GetProperty("content")[0].GetProperty("text").GetString() ?? throw new InvalidOperationException();
    }
}