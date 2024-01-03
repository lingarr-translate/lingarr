using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Models;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DirectoryController : ControllerBase
{
    private readonly DirectoryService _directoryService;

    public DirectoryController(DirectoryService directoryService)
    {
        _directoryService = directoryService;
    }

    [HttpGet("List")]
    public IActionResult GetDirectoryList([FromQuery] string mediaType = "movies")
    {
        try
        {
            List<DirectoryItem> directoryList = _directoryService.GetDirectoryList(mediaType);
            return Ok(directoryList);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
