using Lingarr.Core.Entities;
using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Services;

public interface IProgressService
{
    /// <summary>
    /// Emits a progress update for a specific job and updates the database if the job is completed.
    /// </summary>
    /// <param name="translationRequest"></param>
    /// <param name="progress">The current progress of the job (0-100).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Emit(TranslationRequest translationRequest, int progress);

    /// <summary>
    /// Saves a translated line to the database and broadcasts it via SignalR.
    /// </summary>
    Task EmitLine(TranslationRequest request, int position, string source, string target);

    /// <summary>
    /// Saves multiple translated lines to the database and broadcasts them via SignalR.
    /// </summary>
    Task EmitLines(TranslationRequest request, List<TranslatedLineData> lines);
}