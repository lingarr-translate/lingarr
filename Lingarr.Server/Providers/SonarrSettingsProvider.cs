using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Providers;

public class SonarrSettingsProvider : ISonarrSettingsProvider
{
    private readonly ISettingService _settingService;
    private readonly ILogger<ISonarrSettingsProvider> _logger;

    public SonarrSettingsProvider(
        ISettingService settingService,
        ILogger<ISonarrSettingsProvider> logger)
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