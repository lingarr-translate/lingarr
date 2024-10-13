using System.Text;
using System.Text.Json;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;

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
        var settings = await _settings.GetSettings(["local_ai_model", "local_ai_endpoint"]);
        if (string.IsNullOrEmpty(settings["local_ai_model"]) || string.IsNullOrEmpty(settings["local_ai_endpoint"]))
        {
            throw new InvalidOperationException("Local AI API key or model is not configured.");
        }
        var prompt = $"Translate the following text from {sourceLanguage} to {targetLanguage}. Only provide the translation, without any additional comments or explanations: {text}";
        
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings["local_ai_model"],
            messages = new[]
            {
                new { role = "user", content = prompt }
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
        return jsonResponse.Choices[0].Message.Content ?? throw new InvalidOperationException();
    }
}