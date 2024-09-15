using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Lingarr.Server.Exceptions;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Services.Subtitle;

namespace Lingarr.Server.Services;

public class TranslateService : ITranslateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITranslateService> _logger;

    public TranslateService(
        IHttpClientFactory httpClientFactory,
        ILogger<ITranslateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<List<SubtitleItem>> TranslateAsync(string subtitlePath, string targetLanguage)
    {
        List<SubtitleItem> subtitles;

        var parser = new SubRipParser();
        await using (var fileStream = File.OpenRead(subtitlePath))
        {
            subtitles = parser.ParseStream(fileStream, Encoding.UTF8);
        }

        using var httpClient = _httpClientFactory.CreateClient();
        var libreTranslateApi = Environment.GetEnvironmentVariable("LIBRETRANSLATE_API") ?? "http://libretranslate:5000";

        foreach (var subtitle in subtitles)
        {
            for (var index = 0; index < subtitle.Lines.Count; index++)
            {
                var content = new StringContent(JsonSerializer.Serialize(new
                {
                    q = subtitle.Lines[index],
                    source = "auto",
                    target = targetLanguage,
                    format = "text"
                }), Encoding.UTF8, "application/json");

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await httpClient.PostAsync($"{libreTranslateApi}/translate", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Response Status Code: {StatusCode}", response.StatusCode);
                    _logger.LogError("Response Content: {ResponseContent}", await response.Content.ReadAsStringAsync());
                    throw new TranslationException("Translation failed.");
                }

                var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
                if (result?.TranslatedText != null)
                {
                    subtitle.Lines[index] = result.TranslatedText;
                }
            }
        }

        const string pattern = @"(\.([a-zA-Z]{2,3}))?\.srt$";
        var replacement = $".{targetLanguage}.srt";
        var filePath = Regex.Replace(subtitlePath, pattern, replacement);

        var writer = new SubRipWriter();
        await using (var fileStream = File.OpenWrite(filePath))
        {
            await writer.WriteStreamAsync(fileStream, subtitles);
        }

        return subtitles;
    }
}