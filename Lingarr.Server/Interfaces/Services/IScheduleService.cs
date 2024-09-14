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
}