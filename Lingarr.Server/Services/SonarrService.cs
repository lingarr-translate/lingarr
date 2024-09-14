using System.Text.Json;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Integrations;
using Lingarr.Server.Providers;

namespace Lingarr.Server.Services;

public class SonarrService : ISonarrService
{
    private readonly HttpClient _httpClient;
    private readonly SonarrSettingsProvider _settingsProvider;

    public SonarrService(
        HttpClient httpClient,
        SonarrSettingsProvider settingsProvider)
    {
        _httpClient = httpClient;
        _settingsProvider = settingsProvider;
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrShow>?> GetShows()
    {
        var apiUrl = "/api/v3/series/";
        return await GetApiResponse<List<SonarrShow>>(apiUrl) ?? null;
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrEpisode>?> GetEpisodes(int seriesNumber, int seasonNumber)
    {
        var apiUrl = $"/api/v3/episode?seriesId={seriesNumber}&seasonNumber={seasonNumber}&includeImages=true";
        return await GetApiResponse<List<SonarrEpisode>>(apiUrl) ?? null;
    }
    
    /// <inheritdoc />
    public async Task<SonarrEpisodePath?> GetEpisodePath(int episodeNumber)
    {
        var apiUrl = $"/api/v3/episode/{episodeNumber}";
        return await GetApiResponse<SonarrEpisodePath>(apiUrl) ?? null;
    }
    
    /// <summary>
    /// Asynchronously sends an HTTP GET request to the specified Sonarr API endpoint and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The expected type of the response data.</typeparam>
    /// <param name="apiUrl">The Sonarr API endpoint to send the request to.</param>
    /// <returns>
    /// The task result contains the deserialized response  of type <typeparamref name="T"/>, or <c>null</c>
    /// if the request fails or the deserialization is unsuccessful.
    /// </returns>
    private async Task<T?> GetApiResponse<T>(string apiUrl)
    {
        var settings = await _settingsProvider.GetSonarrSettings();
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