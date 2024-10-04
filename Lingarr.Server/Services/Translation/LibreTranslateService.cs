using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;

namespace Lingarr.Server.Services.Translation;

public class LibreTranslateService : TranslationServiceBase
{
    private readonly HttpClient _httpClient;

    public LibreTranslateService(
        HttpClient httpClient,
        ISettingService settings,
        ILogger<LibreTranslateService> logger) : base(settings, logger)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var libreTranslateApi = await _settings.GetSetting("libretranslate_url") ?? "http://libretranslate:5000";
        
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            q = text,
            source = sourceLanguage,
            target = targetLanguage,
            format = "text"
        }), Encoding.UTF8, "application/json");

        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _httpClient.PostAsync($"{libreTranslateApi}/translate", content);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
            _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync());
            throw new TranslationException("Translation using LibreTranslate failed.");
        }

        var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
        return result?.TranslatedText ?? string.Empty;
    }
}