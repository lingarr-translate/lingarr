using Lingarr.Core.Entities;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class SettingController : ControllerBase
{
    private readonly ISettingService _settingService;

    public SettingController(ISettingService settingService)
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
    public async Task<ActionResult<string?>> GetSetting(string key)
    {
        var value = await _settingService.GetSetting(key);
        if (value != null)
        {
            return Ok(value);
        }

        return BadRequest("Setting not found");
    }

    /// <summary>
    /// Retrieves the values of multiple settings by their keys.
    /// </summary>
    /// <param name="keys">A list of keys for the settings to retrieve.</param>
    /// <returns>Returns an HTTP 200 OK response with a dictionary of setting keys and values.</returns>
    [HttpPost("multiple/get")]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings([FromBody] IEnumerable<string> keys)
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
    public async Task<ActionResult<bool>> SetSetting([FromBody] Setting setting)
    {
        var value = await _settingService.SetSetting(setting.Key, setting.Value);
        if (value)
        {
            return Ok();
        }

        return BadRequest("Setting not found or could not be updated.");
    }

    /// <summary>
    /// Updates or creates multiple settings with the specified keys and values.
    /// </summary>
    /// <param name="settings">A dictionary where the keys are setting keys and the values are the new values to assign.</param>
    /// <returns>Returns an HTTP 200 OK response if all settings were successfully updated or created; otherwise,
    /// an HTTP 400 Bad Request response.</returns>
    [HttpPost("multiple/set")]
    public async Task<ActionResult<bool>> SetSettings([FromBody] Dictionary<string, string> settings)
    {
        var success = await _settingService.SetSettings(settings);
        if (success)
        {
            return Ok();
        }

        return BadRequest("Some settings were not found or could not be updated.");
    }

    /// <summary>
    /// Encrypts and stores a single sensitive setting value.
    /// </summary>
    /// <param name="setting">The setting object containing the key and plaintext value to encrypt and store.</param>
    /// <returns>Returns an HTTP 200 OK response if the setting was stored; otherwise an HTTP 400 Bad Request.</returns>
    [HttpPost("encrypted")]
    public async Task<ActionResult> SetEncryptedSetting([FromBody] Setting setting)
    {
        var success = await _settingService.SetEncryptedSetting(setting.Key, setting.Value);
        return success ? Ok() : BadRequest("Setting could not be updated.");
    }

    /// <summary>
    /// Retrieves and decrypts sensitive settings values.
    /// Returns an empty string for keys not found or that contain a non-decryptable value.
    /// </summary>
    /// <param name="keys">A list of setting keys to retrieve and decrypt.</param>
    /// <returns>Returns an HTTP 200 OK response with a dictionary of decrypted setting values.</returns>
    [HttpPost("multiple/encrypted/get")]
    public async Task<ActionResult<Dictionary<string, string>>> GetEncryptedSettings(
        [FromBody] IEnumerable<string> keys)
    {
        var settings = await _settingService.GetEncryptedSettings(keys);
        return Ok(settings);
    }
}