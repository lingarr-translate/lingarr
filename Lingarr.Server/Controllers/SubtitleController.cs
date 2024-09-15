using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubtitleController : ControllerBase
{
    private readonly ISubtitleService _subtitleService;

    public SubtitleController(ISubtitleService subtitleService)
    {
        _subtitleService = subtitleService;
    }
    
    /// <summary>
    /// Retrieves a list of subtitle files located at the specified path.
    /// </summary>
    /// <param name="path">The directory path to search for subtitle files.This path is relative to the media folder
    /// and should not start with a forward slash.</param>
    /// <returns>Returns an HTTP 200 OK response with a list of <see cref="Subtitles"/> objects found at the specified path.</returns>
    [HttpGet("collect")]
    public async Task<IActionResult> Collect([FromQuery] string path)
    {
        var value = await _subtitleService.Collect(path);
        return Ok(value);
    }
}