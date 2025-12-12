using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class TranslationController : ControllerBase
{
    private readonly ISettingService _settingService;
    private readonly ILogger<TranslationController> _logger;
    private const string ConfigPath = "/app/config/translations";
    private const string DefaultPath = "/app/Statics/Translations";

    public TranslationController(
        ISettingService settingService,
        ILogger<TranslationController> logger)
    {
        _settingService = settingService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("languages")]
    public ActionResult GetAvailableLanguages()
    {
        try
        {
            var languages = new[]
            {
                new { code = "en", name = "English" },
                new { code = "nl", name = "Dutch" }
            };

            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available languages");
            return StatusCode(500, "Error retrieving available languages");
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetTranslations()
    {
        try
        {
            var locale = await _settingService.GetSetting("locale") ?? "en";
            var filePath = Path.Combine(ConfigPath, $"{locale}.json");
            
            // If file doesn't exist in config, try default path
            if (!System.IO.File.Exists(filePath))
            {
                filePath = Path.Combine(DefaultPath, $"{locale}.json");
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Translations for locale '{locale}' not found");
                }
            }

            var jsonContent = System.IO.File.ReadAllText(filePath);
            var translations = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
            
            var response = new {
                locale = locale,
                messages = translations
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving translations");
            return StatusCode(500, "Error retrieving translations");
        }
    }
}