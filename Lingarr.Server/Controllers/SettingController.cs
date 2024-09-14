using Lingarr.Core.Entities;
using Lingarr.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingController : ControllerBase
{
    private readonly SettingService _settingService;

    public SettingController(SettingService settingService)
    {
        _settingService = settingService;
    }
    
    /// <summary>
    /// Retrieves the value of a specific setting by its key.
    /// </summary>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>Returns an HTTP 200 OK response with the setting value if found; otherwise,
    /// an HTTP 404 Not Found response.</returns>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        var value = await _settingService.GetSetting(key);
        if (value != null)
        {
            return Ok(value);
        }

        return NotFound();
    }

    /// <summary>
    /// Retrieves the values of multiple settings by their keys.
    /// </summary>
    /// <param name="keys">A list of keys for the settings to retrieve.</param>
    /// <returns>Returns an HTTP 200 OK response with a dictionary of setting keys and values.</returns>
    [HttpGet("multiple")]
    public async Task<IActionResult> GetSettings([FromQuery] IEnumerable<string> keys)
    {
        var settings = await _settingService.GetSettings(keys);
        return Ok(settings);
    }
    
    /// <summary>
    /// Updates or creates a setting with the specified key and value.
    /// </summary>
    /// <param name="setting">The setting object containing the key and value to be updated or created.</param>
    /// <returns>Returns an HTTP 200 OK response if the setting was successfully updated or created; otherwise,
    /// an HTTP 404 Not Found response.</returns>
    [HttpPost]
    public async Task<IActionResult> SetSetting([FromBody] Setting setting)
    {
        var value = await _settingService.SetSetting(setting.Key, setting.Value);
        if (value)
        {
            return Ok();
        }

        return NotFound();
    }

    /// <summary>
    /// Updates or creates multiple settings with the specified keys and values.
    /// </summary>
    /// <param name="settings">A dictionary where the keys are setting keys and the values are the new values to assign.</param>
    /// <returns>Returns an HTTP 200 OK response if all settings were successfully updated or created; otherwise,
    /// an HTTP 400 Bad Request response.</returns>
    [HttpPost("multiple")]
    public async Task<IActionResult> SetSettings([FromBody] Dictionary<string, string> settings)
    {
        var success = await _settingService.SetSettings(settings);
        if (success)
        {
            return Ok();
        }

        return BadRequest("Some settings were not found or could not be updated.");
    }
}