using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Providers;

public class IntegrationSettingsProvider : IIntegrationSettingsProvider
{
    private readonly ISettingService _settingService;
    private readonly ILogger<IIntegrationSettingsProvider> _logger;

    public IntegrationSettingsProvider(
        ISettingService settingService,
        ILogger<IIntegrationSettingsProvider> logger)
    {
        _settingService = settingService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<IntegrationSettings?> GetSettings(IntegrationSettingKeys settingKeys)
    {
        var settings = await _settingService.GetSettings([ settingKeys.Url, settingKeys.ApiKey ]);
        
        var variables = new Dictionary<string, string?>
        {
            { "Url", settings.TryGetValue(settingKeys.Url, out var url) ? url : string.Empty},
            { "ApiKey", settings.TryGetValue(settingKeys.ApiKey, out var apiKey) ? apiKey : string.Empty }
        };

        var missingVariables = variables.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList();
        if (missingVariables.Any())
        {
            _logger.LogError("API settings are not configured correctly.");
            return null;
        }

        return new IntegrationSettings 
        { 
            Url = variables["Url"], 
            ApiKey = variables["ApiKey"] 
        };
    }
}

