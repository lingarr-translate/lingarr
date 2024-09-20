using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services;

public class SonarrService : ISonarrService
{
    private readonly IIntegrationService _integrationService;

    public SonarrService(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrShow>?> GetShows()
    {
        var apiUrl = "/api/v3/series/";
        return await _integrationService.GetApiResponse<List<SonarrShow>>(
            apiUrl,
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrEpisode>?> GetEpisodes(int seriesNumber, int seasonNumber)
    {
        var apiUrl = $"/api/v3/episode?seriesId={seriesNumber}&seasonNumber={seasonNumber}&includeImages=true";
        return await _integrationService.GetApiResponse<List<SonarrEpisode>>(
            apiUrl,
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
    
    /// <inheritdoc />
    public async Task<SonarrEpisodePath?> GetEpisodePath(int episodeNumber)
    {
        var apiUrl = $"/api/v3/episode/{episodeNumber}";
        return await _integrationService.GetApiResponse<SonarrEpisodePath>(
            apiUrl,
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
}