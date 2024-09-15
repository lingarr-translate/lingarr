using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Providers;

public class RadarrSettingsProvider : IRadarrSettingsProvider
{
    private readonly ISettingService _settingService;
    private readonly ILogger<IRadarrSettingsProvider> _logger;

    public RadarrSettingsProvider(
        ISettingService settingService,
        ILogger<IRadarrSettingsProvider> logger)
    {
        _settingService = settingService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<RadarrSettings?> GetRadarrSettings()
    {
        var settings = await _settingService.GetSettings(["radarr_url", "radarr_api_key"]);
        
        var variables = new Dictionary<string, string?>
        {
            { "Url", settings.TryGetValue("radarr_url", out var url) ? url : string.Empty},
            { "ApiKey", settings.TryGetValue("radarr_api_key", out var apiKey) ? apiKey : string.Empty }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            _logger.LogError("Radarr API settings are not configured correctly.");
            return null;
        }

        return new RadarrSettings { Url = variables["Url"], ApiKey = variables["ApiKey"] };
    }
}