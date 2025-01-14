using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service responsible for initializing and configuring recurring jobs for media indexing.
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// Initializes the scheduling service by configuring recurring jobs for media indexing.
    /// </summary>
    /// <remarks>
    /// This method should perform the following steps:
    /// 1. Retrieve movie and show schedule settings from the configuration service.
    /// 2. Configure recurring jobs for fetching movies and shows based on the retrieved settings.
    /// 3. Use a job scheduling system to schedule these jobs with the specified cron expressions.
    /// </remarks>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task Initialize();
    
    
    /// <summary>
    /// Gets a list of recurring jobs with their current status.
    /// </summary>
    /// <returns>A list of recurring job statuses</returns>
    List<RecurringJobStatus> GetRecurringJobs();
    
    /// <summary>
    /// Gets the current state of a specific job.
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>The current state of the job</returns>
    string GetJobState(string jobId);
    
    /// <summary>
    /// Updates the state of a job and notifies clients.
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <param name="state">The current state</param>
    Task UpdateJobState(string jobId, string state);
}