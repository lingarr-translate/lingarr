﻿using Hangfire;
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
        _backgroundJobClient.Enqueue<GetMovieJob>(job => job.Execute(JobCancellationToken.Null));
        return Ok();
    }

    /// <summary>
    /// Enqueues a background job to retrieve show data.
    /// </summary>
    /// <returns>Returns an HTTP 200 OK response indicating that the job has been successfully enqueued.</returns>
    [HttpGet("job/show")]
    public IActionResult StartShowJob()
    {
        _backgroundJobClient.Enqueue<GetShowJob>(job => job.Execute(JobCancellationToken.Null));
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
    public IActionResult RemoveJob(string jobId)
    {
        bool result = _backgroundJobClient.Delete(jobId);
    
        if (result)
        {
            return Ok($"Job {jobId} has been successfully removed or stopped.");
        }

        return NotFound($"Job {jobId} not found or could not be removed.");
    }
}