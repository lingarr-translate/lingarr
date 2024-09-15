using Microsoft.AspNetCore.Mvc;
using Lingarr.Core;
using Lingarr.Core.Models;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VersionInfo>> Get()
    {
        var versionInfo = await LingarrVersion.CheckForUpdates();
        return Ok(versionInfo);
    }
}