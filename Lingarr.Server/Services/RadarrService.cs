using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services;

public class RadarrService : IRadarrService
{
    private readonly IIntegrationService _integrationService;

    public RadarrService(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    /// <inheritdoc />
    public async Task<List<RadarrMovie>?> GetMovies()
    {
        return await _integrationService.GetApiResponse<List<RadarrMovie>>(
            "/api/v3/movie/",
            new IntegrationSettingKeys
            {
                Url = "radarr_url",
                ApiKey = "radarr_api_key"
            });
    }
}