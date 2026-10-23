using Lingarr.Core.Data;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class SubtitleController : ControllerBase
{
    private readonly ISubtitleService _subtitleService;
    private readonly LingarrDbContext _dbContext;

    public SubtitleController(ISubtitleService subtitleService, LingarrDbContext dbContext)
    {
        _subtitleService = subtitleService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves a list of subtitle files located in the folder of the specified season.
    /// </summary>
    /// <param name="request">The id of the season to search for subtitle files.</param>
    /// <returns>Returns an HTTP 200 OK response with a list of <see cref="Subtitles"/> objects found in the season folder,
    /// or an HTTP 404 Not Found response when the season does not exist.</returns>
    [HttpPost("season")]
    public async Task<ActionResult<List<Subtitles>>> GetSeasonSubtitles([FromBody] SeasonSubtitlesRequest request)
    {
        var season = await _dbContext.Seasons.FindAsync(request.SeasonId);
        if (season == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(season.Path))
        {
            return Ok(new List<Subtitles>());
        }

        var value = await _subtitleService.GetAllSubtitles(season.Path);
        return Ok(value);
    }
}
