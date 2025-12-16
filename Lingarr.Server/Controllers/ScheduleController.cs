using Hangfire;
using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Jobs;
using Lingarr.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ScheduleController(
        IScheduleService scheduleService,
        IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Retrieves information about all jobs in the system.
    /// </summary>
    /// <returns>A list of job information including status, progress, and other details.</returns>
    [HttpGet("jobs")]
    public IActionResult RecurringJobs()
    {
        var jobs = _scheduleService.GetRecurringJobs();
        return Ok(jobs);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpPost("job/start")]
    public IActionResult StartJob([FromBody] StartJobRequest recurringJob)
    {
        RecurringJob.TriggerJob(recurringJob.JobName);
        return Ok();
    }

    /// <summary>
    /// Enqueues a background job to automate translation.
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that the job has been successfully enqueued.</returns>
    [HttpGet("job/automation")]
    public IActionResult StartAutomationJob()
    {
        _backgroundJobClient.Enqueue<AutomatedTranslationJob>(job => job.Execute());
        return Ok();
    }

    /// <summary>
    /// Enqueues a background job to retrieve movie data.
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that the job has been successfully enqueued.</returns>
    [HttpGet("job/movie")]
    public IActionResult StartMovieJob()
    {
        _backgroundJobClient.Enqueue<SyncMovieJob>(job => job.Execute());
        return Ok();
    }

    /// <summary>
    /// Enqueues a background job to retrieve show data.
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that the job has been successfully enqueued.</returns>
    [HttpGet("job/show")]
    public IActionResult StartShowJob()
    {
        _backgroundJobClient.Enqueue<SyncShowJob>(job => job.Execute());
        return Ok();
    }
    
    /// <summary>
    /// Attempts to remove a background job from the queue or stop it if it's already running.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job to be removed.</param>
    /// <returns>
    /// Returns an HTTP 200 OK response with a success message if the job was successfully removed or stopped.
    /// Returns an HTTP 404 Not Found response if the job doesn't exist or couldn't be removed.
    /// </returns>
    [HttpDelete("job/remove/{jobId}")]
    public ActionResult<string> RemoveJob(string jobId)
    {
        bool result = _backgroundJobClient.Delete(jobId);
    
        if (result)
        {
            return Ok($"Job {jobId} has been successfully removed or stopped.");
        }

        return NotFound($"Job {jobId} not found or could not be removed.");
    }
    
    /// <summary>
    /// Enqueues a background job to reindex shows and movies
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that both jobs has been successfully enqueued.</returns>
    [HttpPost("job/index/movies")]
    public IActionResult IndexMovies()
    {
        _backgroundJobClient.Enqueue<SyncMovieJob>(job => job.Execute());
        return Ok();
    }
    
    /// <summary>
    /// Enqueues a background job to reindex shows and movies
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that both jobs has been successfully enqueued.</returns>
    [HttpPost("job/index/shows")]
    public IActionResult IndexShows()
    {
        _backgroundJobClient.Enqueue<SyncShowJob>(job => job.Execute());
        return Ok();
    }
}