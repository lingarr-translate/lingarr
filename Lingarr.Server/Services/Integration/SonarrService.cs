using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Integration;

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
        return await _integrationService.GetApiResponse<List<SonarrShow>>(
            "/api/v3/series/",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
    
    /// <inheritdoc />
    public async Task<List<SonarrEpisode>?> GetEpisodes(int seriesNumber, int seasonNumber)
    {
        return await _integrationService.GetApiResponse<List<SonarrEpisode>>(
            $"/api/v3/episode?seriesId={seriesNumber}&seasonNumber={seasonNumber}&includeImages=true",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }

    /// <inheritdoc />
    public async Task<SonarrEpisode?> GetEpisode(int episodeNumber)
    {
        return await _integrationService.GetApiResponse<SonarrEpisode>(
            $"/api/v3/episode/{episodeNumber}?includeImages=true",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
    
    /// <inheritdoc />
    public async Task<SonarrEpisodePath?> GetEpisodePath(int episodeId)
    {
        return await _integrationService.GetApiResponse<SonarrEpisodePath>(
            $"/api/v3/episode/{episodeId}",
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
    }
}