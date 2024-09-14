using Hangfire;
using Lingarr.Server.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ScheduleController(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Enqueues a background job to retrieve movie data.
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that the job has been successfully enqueued.</returns>
    [HttpGet("job/movie")]
    public IActionResult StartMovieJob()
    {
        _backgroundJobClient.Enqueue<GetMovieJob>(x => x.Execute());
        return Ok();
    }

    /// <summary>
    /// Enqueues a background job to retrieve show data.
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that the job has been successfully enqueued.</returns>
    [HttpGet("job/show")]
    public IActionResult StartShowJob()
    {
        _backgroundJobClient.Enqueue<GetShowJob>(x => x.Execute());
        return Ok();
    }
}