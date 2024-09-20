﻿using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Models.FileSystem;
using Lingarr.Server.Jobs;
using Hangfire;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public TranslateController(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }
    
    /// <summary>
    /// Initiates a translation job for the provided subtitle data.
    /// </summary>
    /// <param name="translateAbleSubtitle">The subtitle data to be translated. This includes the subtitle content
    /// and any necessary translation details.</param>
    /// <returns>Returns an HTTP 200 OK response if the job was successfully enqueued.</returns>
    [HttpPost]
    public IActionResult Translate([FromBody] TranslateAbleSubtitle translateAbleSubtitle)
    {
        string jobId = Guid.NewGuid().ToString();
        _backgroundJobClient.Enqueue<TranslateJob>(x => x.Execute(jobId, translateAbleSubtitle));
        return Ok(new { JobId = jobId });
    }
}