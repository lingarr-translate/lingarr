using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IIntegrationSettingsProvider _settingsProvider;
    private readonly IImageService _imageService;

    public ImageController(IIntegrationSettingsProvider settingsProvider, IImageService imageService)
    {
        _settingsProvider = settingsProvider;
        _imageService = imageService;
    }
    
    /// <summary>
    /// Retrieves images related to a TV show based on the specified path.
    /// </summary>
    /// <param name="path">The API path for accessing the TV show images. This path is appended to the Sonarr API URL.</param>
    /// <returns>Returns an HTTP response with the images if the request is successful; otherwise, it returns an error response.</returns>
    [HttpGet("show/{*path}")]
    public async Task<IActionResult> ShowImages(string path)
    {
        var settings = await _settingsProvider.GetSettings(
            new IntegrationSettingKeys
            {
                Url = "sonarr_url",
                ApiKey = "sonarr_api_key"
            });
        if (settings == null)
        {
            return BadRequest("Sonarr settings are not configured correctly.");
        }
        var url = $"{settings.Url}/api/v3/{path}?apikey={settings.ApiKey}";

        return await _imageService.GetApiResponse(url);
    }
    
    /// <summary>
    /// Retrieves images related to a movie based on the specified path.
    /// </summary>
    /// <param name="path">The API path for accessing the movie images. This path is appended to the Radarr API URL.</param>
    /// <returns>Returns an HTTP response with the images if the request is successful; otherwise, it returns an error response.</returns>
    [HttpGet("movie/{*path}")]
    public async Task<IActionResult> MovieImages(string path)
    {
        var settings = await _settingsProvider.GetSettings(
            new IntegrationSettingKeys
            {
                Url = "radarr_url",
                ApiKey = "radarr_api_key"
            });
        if (settings == null)
        {
            return BadRequest("Radarr settings are not configured correctly.");
        }
        var url = $"{settings.Url}/api/v3/{path}?apikey={settings.ApiKey}";

        return await _imageService.GetApiResponse(url);
    }
}