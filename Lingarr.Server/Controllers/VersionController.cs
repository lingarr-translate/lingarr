using Microsoft.AspNetCore.Mvc;
using Lingarr.Core;
using Lingarr.Core.Models;
using Lingarr.Server.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    /// <summary>
    /// Retrieves the current version information and checks for available updates.
    /// </summary>
    /// <returns>A Task containing an ActionResult with VersionInfo data. Returns HTTP 200 OK on success.</returns>
    /// <response code="200">Returns the version information</response>
    /// <response code="500">If there was an error checking for updates</response>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<VersionInfo>> Get()
    {
        var versionInfo = await LingarrVersion.CheckForUpdates();
        return Ok(versionInfo);
    }
}