using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MappingController : ControllerBase
{
    private readonly IMappingService _mappingService;

    public MappingController(IMappingService mappingService)
    {
        _mappingService = mappingService;
    }
    /// <summary>
    /// Retrieves all path mappings from the system.
    /// </summary>
    /// <returns>A list of <see cref="PathMapping"/> objects representing the current mappings.</returns>
    /// <response code="200">Returns the list of path mappings.</response>
    [HttpGet("get")]
    public async Task<ActionResult<List<PathMapping>>> GetMapping()
    {
        var mappings = await _mappingService.GetMapping();
        return Ok(mappings);
    }
    
    /// <summary>
    /// Updates or creates path mappings in the system.
    /// </summary>
    /// <param name="mappings">The list of path mappings to set.</param>
    /// <returns>An IActionResult indicating success or failure.</returns>
    /// <response code="200">The mappings were successfully updated.</response>
    /// <response code="400">If the mappings are invalid.</response>
    [HttpPost("set")]
    public async Task<IActionResult> SetMapping([FromBody] List<PathMapping> mappings)
    {
        await _mappingService.SetMapping(mappings);
        return Ok();
    }
}