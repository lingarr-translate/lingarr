using Lingarr.Server.Interfaces.Providers;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly ISonarrSettingsProvider _sonarrSettingsProvider;
    private readonly IRadarrSettingsProvider _radarrSettingsProvider;
    private readonly IImageService _imageService;

    public ImageController(ISonarrSettingsProvider sonarrSettingsProvider,
        IRadarrSettingsProvider radarrSettingsProvider,
        IImageService imageService)
    {
        _sonarrSettingsProvider = sonarrSettingsProvider;
        _radarrSettingsProvider = radarrSettingsProvider;
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
        var settings = await _sonarrSettingsProvider.GetSonarrSettings();
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
        var settings = await _radarrSettingsProvider.GetRadarrSettings();
        var url = $"{settings.Url}/api/v3/{path}?apikey={settings.ApiKey}";

        return await _imageService.GetApiResponse(url);
    }
}