using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(
        ITelemetryService telemetryService,
        ILogger<TelemetryController> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    [HttpGet("preview")]
    public async Task<ActionResult<TelemetryPayload>> PreviewTelemetry()
    {
        try
        {
            var payload = await _telemetryService.GenerateTelemetryPayload();
            return Ok(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating telemetry preview");
            return StatusCode(500, new { error = "Failed to generate telemetry preview" });
        }
    }
}
