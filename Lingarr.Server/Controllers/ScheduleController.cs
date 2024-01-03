using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
    private readonly SchedulerService _schedulerService;

    public ScheduleController(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    [HttpPost("EnableJob")]
    public async Task<IActionResult> EnableJob([FromBody] int intervalInSeconds = 900)
    {
        await _schedulerService.StartAsync(intervalInSeconds);
        return Ok();
    }

    [HttpPost("DisableJob")]
    public async Task<IActionResult> DisableJob()
    {
        await _schedulerService.StopAsync();
        return Ok();
    }

    [HttpPost("ConfigureJob")]
    public async Task<IActionResult> ConfigureJob([FromBody] int intervalInSeconds)
    {
        await _schedulerService.UpdateInterval(intervalInSeconds);
        return Ok();
    }
}
