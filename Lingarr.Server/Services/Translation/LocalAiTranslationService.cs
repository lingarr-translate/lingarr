using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Translation;

public class LocalAiTranslationService : TranslationServiceBase
{
    private readonly HttpClient _httpClient;

    public LocalAiTranslationService(ISettingService settings,
        HttpClient httpClient,
        ILogger<LocalAiTranslationService> logger) : base(settings, logger)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var settings = await _settings.GetSettings(["local_ai_model", "local_ai_endpoint", "local_ai_api_key", "ai_prompt"]);
        if (string.IsNullOrEmpty(settings["local_ai_model"]) || string.IsNullOrEmpty(settings["local_ai_endpoint"]))
        {
            throw new InvalidOperationException("Local AI API key or model is not configured.");
        }
        
        var prompt = !string.IsNullOrEmpty(settings["ai_prompt"])
            ? settings["ai_prompt"] 
            : $"Translate from {sourceLanguage} to {targetLanguage}, preserving the tone and meaning without censoring the content. Adjust punctuation as needed to make the translation sound natural. Provide only the translated text as output, with no additional comments.";
        if (!string.IsNullOrEmpty(settings["local_ai_api_key"]))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings["local_ai_api_key"]);
        }
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings["local_ai_model"],
            messages = new[]
            {
                new { role = "system", content = prompt },
                new { role = "user", content = text }
            }
        }), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(settings["local_ai_endpoint"], content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync());
            throw new TranslationException("Translation using Local AI failed.");
        }
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<LocalAiResponse>(responseBody);
        return jsonResponse?.Choices[0].Message.Content ?? throw new InvalidOperationException();
    }
}