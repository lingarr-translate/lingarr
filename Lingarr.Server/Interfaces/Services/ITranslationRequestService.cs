using DeepL;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Interfaces.Services;

public interface ITranslationRequestService
{
    /// <summary>
    /// Creates a new translation request for a subtitle file and enqueues it for processing.
    /// </summary>
    /// <param name="translateAbleSubtitle">Details of the subtitle to be translated, including source and target languages</param>
    /// <returns>The ID of the created translation request</returns>
    Task<int> CreateRequest(TranslateAbleSubtitle translateAbleSubtitle);
    
    /// <summary>
    /// Retrieves the count of active translation requests.
    /// </summary>
    /// <returns>Number of translation requests that are neither Cancelled nor Completed</returns>
    Task<int> GetActiveCount();
    
    /// <summary>
    /// Updates the active count and notifies connected clients via SignalR.
    /// </summary>
    /// <returns>The current count of active translation requests</returns>
    Task<int> UpdateActiveCount();
    
    /// <summary>
    /// Resumes all pending and in-progress translation requests by re-enqueueing them in the job queue.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResumeTranslationRequests();
    
    /// <summary>
    /// Retrieves a paginated list of translation requests with optional filtering and sorting.
    /// </summary>
    /// <param name="searchQuery">Optional search term to filter requests by title</param>
    /// <param name="orderBy">Property to sort by: "Title", "CreatedAt", or "CompletedAt"</param>
    /// <param name="ascending">Sort direction</param>
    /// <param name="pageNumber">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>
    /// A PagedResult containing the requested translation requests and pagination information
    /// </returns>
    Task<PagedResult<TranslationRequest>> GetTranslationRequests(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Cancels an existing translation request and its associated background job.
    /// </summary>
    /// <param name="cancelRequest">The translation request to cancel</param>
    /// <returns>
    /// A message indicating the result of the cancellation operation, or null if the request wasn't found
    /// </returns>
    Task<string?> CancelTranslationRequest(
        TranslationRequest cancelRequest
    );

    /// <summary>
    /// Updates the status and job ID of an existing translation request.
    /// </summary>
    /// <param name="translationRequest">The translation request to update</param>
    /// <param name="jobId">The ID of the associated Hangfire background job</param>
    /// <param name="status">The new status to set</param>
    /// <returns>The updated translation request</returns>
    /// <exception cref="NotFoundException">Thrown when the specified translation request is not found</exception>
    Task<TranslationRequest> UpdateTranslationRequest(
        TranslationRequest translationRequest,
        string jobId,
        TranslationStatus status);
}