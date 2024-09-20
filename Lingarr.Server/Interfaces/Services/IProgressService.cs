namespace Lingarr.Server.Interfaces.Services;

public interface IProgressService
{
    /// <summary>
    /// Emits a progress update for a specific job and updates the database if the job is completed.
    /// </summary>
    /// <param name="jobId">The unique identifier for the job.</param>
    /// <param name="progress">The current progress of the job (0-100).</param>
    /// <param name="completed">Indicates whether the job is completed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Emit(string jobId, int progress, bool completed);
}