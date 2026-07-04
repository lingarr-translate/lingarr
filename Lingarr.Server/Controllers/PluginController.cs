using Lingarr.Contracts.Models;
using Lingarr.Contracts.Plugins;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Services.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class PluginController : ControllerBase
{
    private readonly IPluginRegistry _registry;
    private readonly ISettingService _settings;
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly ILogger<PluginController> _logger;

    public PluginController(
        IPluginRegistry registry,
        ISettingService settings,
        ITranslationServiceFactory translationServiceFactory,
        ILogger<PluginController> logger)
    {
        _registry = registry;
        _settings = settings;
        _translationServiceFactory = translationServiceFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists every registered plugin together with the settings fields needed to render its
    /// configuration form.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<PluginResponse>> List()
    {
        var responses = new List<PluginResponse>();
        foreach (var plugin in _registry.All)
        {
            responses.Add(BuildResponse(plugin));
        }
        return Ok(responses);
    }

    /// <summary>
    /// Returns the manifest for one provider.
    /// </summary>
    [HttpGet("{provider}/manifest")]
    public ActionResult<PluginResponse> GetManifest(string provider)
    {
        var plugin = _registry.Find(provider);
        if (plugin is null)
        {
            return NotFound();
        }

        return Ok(BuildResponse(plugin));
    }

    /// <summary>
    /// Fetches the provider's AI model catalogue for manifest dropdowns. Returns 404 when the
    /// provider is unknown; providers without a model catalogue (for example DeepL or
    /// LibreTranslate) return an empty response.
    /// </summary>
    [HttpGet("{provider}/models")]
    public async Task<ActionResult<ModelsResponse>> GetModels(string provider)
    {
        var plugin = _registry.Find(provider);
        if (plugin is null)
        {
            return NotFound();
        }

        try
        {
            var translationService = _translationServiceFactory.CreateTranslationService(plugin.Manifest.Provider);
            var models = await translationService.GetModels();
            return Ok(models);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to list models for provider {Provider}.",
                plugin.Manifest.Provider);
            return Ok(new ModelsResponse
            {
                Message = "Error fetching models: " + exception.Message
            });
        }
    }

    /// <summary>
    /// Returns the configuration status for a provider.
    /// </summary>
    [HttpGet("{provider}/status")]
    public async Task<ActionResult<PluginStatusResponse>> GetStatus(string provider)
    {
        var plugin = _registry.Find(provider);
        if (plugin is null)
        {
            return NotFound();
        }

        var missingFields = new List<string>();
        foreach (var field in plugin.Manifest.Settings)
        {
            if (!field.Required)
            {
                continue;
            }

            string? settingValue;
            if (field.Type == PluginSettingType.Secret)
            {
                settingValue = await _settings.GetEncryptedSetting(field.Key);
            }
            else
            {
                settingValue = await _settings.GetSetting(field.Key);
            }

            if (string.IsNullOrEmpty(settingValue))
            {
                missingFields.Add(field.Key);
            }
        }

        return Ok(new PluginStatusResponse
        {
            Provider = plugin.Manifest.Provider,
            Configured = missingFields.Count == 0,
            MissingFields = missingFields
        });
    }

    private static PluginResponse BuildResponse(RegisteredPlugin plugin)
    {
        return new PluginResponse
        {
            Provider = plugin.Manifest.Provider,
            DisplayName = plugin.Manifest.DisplayName,
            Description = plugin.Manifest.Description,
            IsBuiltIn = plugin.IsBuiltIn,
            SourceFile = plugin.SourceFile,
            Settings = plugin.Manifest.Settings,
            HasRequestTemplate = plugin.Manifest.HasRequestTemplate
        };
    }
}
