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
        var url = await _settingService.GetSetting(settingKeys.Url);
        var apiKey = await _settingService.GetEncryptedSetting(settingKeys.ApiKey);

        var variables = new Dictionary<string, string?>
        {
            { "Url", url },
            { "ApiKey", apiKey }
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

