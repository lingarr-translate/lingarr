using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DirectoryController : ControllerBase
{
    private readonly IDirectoryService _directoryService;

    public DirectoryController(IDirectoryService directoryService)
    {
        _directoryService = directoryService;
    }
    
    /// <summary>
    /// Retrieves the contents of a specified directory.
    /// </summary>
    /// <param name="path">The full path to the directory to browse.</param>
    /// <returns>
    /// - 200 OK with a list of DirectoryItem objects representing directory contents
    /// - 404 NotFound if the directory doesn't exist
    /// - 403 Forbidden if access to the directory is denied
    /// - 500 Internal Server Error for unexpected errors
    /// </returns>
    /// <response code="200">Successfully retrieved directory contents</response>
    /// <response code="404">Directory not found at specified path</response>
    /// <response code="403">Access to directory is denied</response>
    /// <response code="500">Internal server error occurred during operation</response>
    [HttpGet("get")]
    public ActionResult<List<DirectoryItem>> BrowseDirectory(string path)
    {
        try
        {
            var contents = _directoryService.GetDirectoryContents(path);
            return Ok(contents);
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound($"Directory not found: {path}");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid($"Access denied to directory: {path}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while browsing the directory: {ex.Message}");
        }
    }
}