using System.Text.Json;
using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services;

public class RadarrService : IRadarrService
{
    private readonly HttpClient _httpClient;
    private readonly IRadarrSettingsProvider _settingsProvider;

    public RadarrService(
        HttpClient httpClient,
        IRadarrSettingsProvider settingsProvider)
    {
        _httpClient = httpClient;
        _settingsProvider = settingsProvider;
    }
    
    /// <inheritdoc />
    public async Task<List<RadarrMovie>?> GetMovies()
    {
        var apiUrl = "/api/v3/movie/";
        return await GetApiResponse<List<RadarrMovie>>(apiUrl) ?? null;
    }
    
    /// <summary>
    /// Asynchronously sends an HTTP GET request to the specified Radarr API endpoint and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The expected type of the response data.</typeparam>
    /// <param name="apiUrl">The Radarr API endpoint to send the request to.</param>
    /// <returns>
    /// The task result contains the deserialized response  of type <typeparamref name="T"/>, or <c>null</c>
    /// if the request fails or the deserialization is unsuccessful.
    /// </returns>
    private async Task<T?> GetApiResponse<T>(string apiUrl)
    {
        var settings = await _settingsProvider.GetRadarrSettings();
        if (settings == null) return default;
        
        var separator = apiUrl.Contains("?") ? "&" : "?";
        var url = $"{settings.Url}{apiUrl}{separator}apikey={settings.ApiKey}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Radarr request failed: {response.StatusCode}: {errorContent}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(responseStream);
    }
}