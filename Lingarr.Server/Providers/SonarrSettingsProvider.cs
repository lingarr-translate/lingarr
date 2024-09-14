using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Models;
using Lingarr.Server.Services;

namespace Lingarr.Server.Providers;

public class SonarrSettingsProvider : ISonarrSettingsProvider
{
    private readonly SettingService _settingService;
    private readonly ILogger<RadarrSettingsProvider> _logger;

    public SonarrSettingsProvider(
        SettingService settingService,
        ILogger<RadarrSettingsProvider> logger)
    {
        _settingService = settingService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<SonarrSettings?> GetSonarrSettings()
    {
        var settings = await _settingService.GetSettings(["sonarr_url", "sonarr_api_key"]);
        
        var variables = new Dictionary<string, string?>
        {
            { "Url", settings.TryGetValue("sonarr_url", out var url) ? url : string.Empty},
            { "ApiKey", settings.TryGetValue("sonarr_api_key", out var apiKey) ? apiKey : string.Empty }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            _logger.LogError("Sonarr API settings are not configured correctly.");
            return null;
        }
        
        return new SonarrSettings { Url = variables["Url"], ApiKey = variables["ApiKey"] };
    }
}