using Lingarr.Core.Entities;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    public async Task<ActionResult<Statistics>> GetStatistics()
    {
        var stats = await _statisticsService.GetStatistics();
        return Ok(stats);
    }
    
    [HttpGet("daily/{days}")]
    public async Task<ActionResult<IEnumerable<DailyStatistics>>> GetDailyStats(int days = 30)
    {
        var stats = await _statisticsService.GetDailyStatistics(days);
        return Ok(stats);
    }
}