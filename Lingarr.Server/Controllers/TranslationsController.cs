using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslationsController : ControllerBase
{
    private readonly ILogger<TranslationsController> _logger;
    private const string ConfigPath = "/app/config/translations";
    private const string DefaultPath = "/app/Statics/Translations";

    public TranslationsController(ILogger<TranslationsController> logger)
    {
        _logger = logger;
    }

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

    [HttpGet("{locale}")]
    public ActionResult GetTranslations(string locale)
    {
        try
        {
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
            return Ok(translations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving translations for locale {Locale}", locale);
            return StatusCode(500, $"Error retrieving translations for locale '{locale}'");
        }
    }

    [HttpPut("{locale}")]
    public ActionResult UpdateTranslations(string locale, [FromBody] Dictionary<string, object> translations)
    {
        try
        {
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }

            var filePath = Path.Combine(ConfigPath, $"{locale}.json");
            var jsonContent = JsonSerializer.Serialize(translations, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            System.IO.File.WriteAllText(filePath, jsonContent);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating translations for locale {Locale}", locale);
            return StatusCode(500, $"Error updating translations for locale '{locale}'");
        }
    }
}