using Lingarr.Contracts.Models;
using Lingarr.Core.Entities;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Services.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class PluginController : ControllerBase
{
    private readonly IPluginRegistry _registry;
    private readonly ITranslationServiceInstanceService _translationServiceInstanceService;
    private readonly ITranslationServiceFactory _translationServiceFactory;
    private readonly ILogger<PluginController> _logger;

    public PluginController(
        IPluginRegistry registry,
        ITranslationServiceInstanceService translationServiceInstanceService,
        ITranslationServiceFactory translationServiceFactory,
        ILogger<PluginController> logger)
    {
        _registry = registry;
        _translationServiceInstanceService = translationServiceInstanceService;
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
    /// Fetches the provider's AI model catalogue for manifest dropdowns, using the settings of the given
    /// translation service. Returns 404 when the provider or service is unknown; providers without a model
    /// catalogue (for example DeepL or LibreTranslate) return an empty response.
    /// </summary>
    [HttpGet("{provider}/models")]
    public async Task<ActionResult<ModelsResponse>> GetModels(string provider, [FromQuery, BindRequired] int serviceId)
    {
        var plugin = _registry.Find(provider);
        if (plugin is null)
        {
            return NotFound();
        }

        var instance = await GetProviderInstance(plugin, serviceId);
        if (instance is null)
        {
            return NotFound();
        }

        try
        {
            var translationService = _translationServiceFactory.CreateTranslationService(instance);
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
    /// Returns the configuration status of a translation service, checked against its settings.
    /// </summary>
    [HttpGet("{provider}/status")]
    public async Task<ActionResult<PluginStatusResponse>> GetStatus(string provider, [FromQuery, BindRequired] int serviceId)
    {
        var plugin = _registry.Find(provider);
        if (plugin is null)
        {
            return NotFound();
        }

        var instance = await GetProviderInstance(plugin, serviceId);
        if (instance is null)
        {
            return NotFound();
        }

        var settings = await _translationServiceInstanceService.GetInstanceSettings(instance.Id)
                       ?? new Dictionary<string, string>();

        var missingFields = new List<string>();
        foreach (var field in plugin.Manifest.Settings)
        {
            if (!field.Required)
            {
                continue;
            }

            if (string.IsNullOrEmpty(settings.GetValueOrDefault(field.Key)))
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

    private async Task<TranslationServiceInstance?> GetProviderInstance(RegisteredPlugin plugin, int serviceId)
    {
        var instance = await _translationServiceInstanceService.GetInstance(serviceId);
        if (instance is null
            || !string.Equals(instance.Provider, plugin.Manifest.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return instance;
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
