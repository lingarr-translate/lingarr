using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Lingarr.Server.Models;
using Lingarr.Server.Parsers;
using Lingarr.Server.Writers;

namespace Lingarr.Server.Services;

public class TranslateService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public TranslateService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<List<SubtitleItem>> Translate(string subtitlePath, string targetLanguage)
    {
        List<SubtitleItem> subtitles;

        var parser = new SubRipParser();
        using (var fileStream = File.OpenRead(subtitlePath))
        {
            subtitles = parser.ParseStream(fileStream, Encoding.UTF8);
        }

        // Translate
        string libreTranslateApi = _configuration["LibreTranslateApi"] ?? "http://libretranslate:5000";

        // Loop through the subtitles
        bool isSuccesfull = true;
        foreach (var subtitle in subtitles)
        {
            // Loop through subtitle lines
            for (int index = 0; index < subtitle.Lines.Count; index++)
            {
                var content = new StringContent(JsonSerializer.Serialize(new
                {
                    q = subtitle.Lines[index],
                    source = "auto",
                    target = targetLanguage,
                    format = "text"
                }).ToString(), Encoding.UTF8, "application/json");

                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await _httpClient.PostAsync($"{libreTranslateApi}/translate", content);
                
                // Response is not successful
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Response Status Code: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
                    isSuccesfull = false;
                    break;
                }

                var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
                if (result?.TranslatedText != null)
                {
                    subtitle.Lines[index] = result.TranslatedText;
                }
            }
            if (!isSuccesfull)
            {
                // @TODO handle failure
                break;
            }
        }

        string pattern = @"(\.([a-zA-Z]{2,3}))?\.srt$";
        string replacement = $".{targetLanguage}.srt";
        string filePath = Regex.Replace(subtitlePath, pattern, replacement);

        var writer = new SubRipWriter();
        using (var fileStream = File.OpenWrite(filePath))
        {
            await writer.WriteStreamAsync(fileStream, subtitles);
        }

        return subtitles;
    }
}