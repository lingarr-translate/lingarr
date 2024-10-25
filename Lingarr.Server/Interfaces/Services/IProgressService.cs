using Lingarr.Core.Entities;

namespace Lingarr.Server.Interfaces.Services;

public interface IProgressService
{
    /// <summary>
    /// Emits a progress update for a specific job and updates the database if the job is completed.
    /// </summary>
    /// <param name="translationRequest"></param>
    /// <param name="progress">The current progress of the job (0-100).</param>
    /// <param name="completed">Indicates whether the job is completed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Emit(TranslationRequest translationRequest, int progress, bool completed);
}