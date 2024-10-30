using System.Text;
using System.Text.Json;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Translation.Base;

namespace Lingarr.Server.Services.Translation;

public class AnthropicService : BaseLanguageService
{
    private readonly HttpClient _httpClient;
    private string? _model;
    private string? _prompt;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

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

            var settings = await _settings.GetSettings(["anthropic_model", "anthropic_api_key", "anthropic_version", "ai_prompt"]);
            
            if (string.IsNullOrEmpty(settings["anthropic_model"]) || 
                string.IsNullOrEmpty(settings["anthropic_api_key"]) || 
                string.IsNullOrEmpty(settings["anthropic_version"]))
            {
                throw new InvalidOperationException("Anthropic API key or model is not configured.");
            }

            _model = settings["anthropic_model"];
            _httpClient.DefaultRequestHeaders.Add("x-api-key", settings["anthropic_api_key"]);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", settings["anthropic_version"]);
            
            _prompt = !string.IsNullOrEmpty(settings["ai_prompt"])
                ? settings["ai_prompt"]
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
    public override async Task<string> TranslateAsync(string? text, string sourceLanguage, string targetLanguage)
    {
        await InitializeAsync(sourceLanguage, targetLanguage);
        
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = 1024,
            messages = new[]
            {
                new { role = "system", content = _prompt },
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