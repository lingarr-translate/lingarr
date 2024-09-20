using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Models;
using Lingarr.Server.Interfaces.Services;
using System.Text.Json;

namespace Lingarr.Server.Services;

public class IntegrationService : IIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IIntegrationSettingsProvider _settingsProvider;

    public IntegrationService(HttpClient httpClient, IIntegrationSettingsProvider settingsProvider)
    {
        _httpClient = httpClient;
        _settingsProvider = settingsProvider;
    }
    
    public async Task<T?> GetApiResponse<T>(string apiUrl, IntegrationSettingKeys settingKeys)
    {
        var settings = await _settingsProvider.GetSettings(settingKeys);
        if (settings == null) return default;
        
        var separator = apiUrl.Contains("?") ? "&" : "?";
        var url = $"{settings.Url}{apiUrl}{separator}apikey={settings.ApiKey}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Integration request failed: {response.StatusCode}: {errorContent}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(responseStream);
    }
}