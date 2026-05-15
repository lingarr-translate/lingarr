using Lingarr.Core.Entities;
using Lingarr.Server.Attributes;
using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;
using Lingarr.Server.Models.TranslationRequests;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class TranslationRequestController : ControllerBase
{
    private readonly ITranslationRequestService _translationRequestService;

    public TranslationRequestController(
        ITranslationRequestService translationRequestService)
    {
        _translationRequestService = translationRequestService;
    }

    /// <summary>
    /// Gets a single translation request with its event timeline
    /// </summary>
    /// <param name="id">The ID of the translation request</param>
    /// <response code="200">Returns the translation request detail</response>
    /// <response code="404">If the translation request was not found</response>
    /// <returns>ActionResult containing the translation request detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<TranslationRequestDetail>> GetTranslationRequest(int id)
    {
        var detail = await _translationRequestService.GetTranslationRequest(id);
        if (detail == null)
        {
            return NotFound();
        }

        return Ok(detail);
    }

    /// <summary>
    /// Gets the collection of currently active (Pending or InProgress) translation
    /// requests. Used by the frontend to seed its state on initial load; subsequent
    /// updates are pushed over SignalR on the <c>ActiveTranslations</c> event.
    /// </summary>
    /// <response code="200">Returns the collection of active translation requests</response>
    /// <returns>ActionResult containing the collection of active translation requests</returns>
    [HttpGet("active")]
    public async Task<ActionResult<List<ActiveTranslation>>> GetActiveTranslations()
    {
        var activeTranslations = await _translationRequestService.GetActiveTranslations();
        return Ok(activeTranslations);
    }

    /// <summary>
    /// Retrieves a paginated list of translation requests with optional filtering and sorting
    /// </summary>
    /// <param name="searchQuery">Optional search term to filter requests</param>
    /// <param name="orderBy">Property name to sort the results by</param>
    /// <param name="ascending">Sort direction; true for ascending, false for descending</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="pageNumber">Page number to retrieve</param>
    /// <response code="200">Returns the paginated list of translation requests</response>
    /// <response code="500">If there was an error checking for updates</response>
    /// <returns>ActionResult containing the paginated list of translation requests</returns>
    [HttpGet("requests")]
    public async Task<ActionResult<PagedResult<TranslationRequest>>> GetTranslationRequests(
        string? searchQuery,
        string? orderBy,
        bool ascending = true,
        int pageSize = 20,
        int pageNumber = 1)
    {
        var value = await _translationRequestService.GetTranslationRequests(
            searchQuery,
            orderBy,
            ascending,
            pageNumber,
            pageSize);

        return Ok(value);
    }

    /// <summary>
    /// Cancels an existing translation request
    /// </summary>
    /// <param name="cancelRequest">The translation request to cancel</param>
    /// <response code="200">Returns the canceled translation request</response>
    /// <response code="404">If the translation request was not found</response>
    /// <response code="500">If there was an error checking for updates</response>
    /// <returns>ActionResult containing the canceled translation request if found, or NotFound if the request doesn't exist</returns>
    [HttpPost("cancel")]
    public async Task<ActionResult<string>> CancelTranslationRequest([FromBody] TranslationRequest cancelRequest)
    {
        var translationRequest = await _translationRequestService.CancelTranslationRequest(cancelRequest);
        if (translationRequest != null)
        {
            return Ok(translationRequest);
        }

        return NotFound(translationRequest);
    }

    /// <summary>
    /// Removes an existing translation request
    /// </summary>
    /// <param name="cancelRequest">The translation request to remove</param>
    /// <response code="200">Returns the removed translation request</response>
    /// <response code="404">If the translation request was not found</response>
    /// <response code="500">If there was an error checking for updates</response>
    /// <returns>ActionResult containing the removed translation request if found, or NotFound if the request doesn't exist</returns>
    [HttpPost("remove")]
    public async Task<ActionResult<string>> RemoveTranslationRequest([FromBody] TranslationRequest cancelRequest)
    {
        var translationRequest = await _translationRequestService.RemoveTranslationRequest(cancelRequest);
        if (translationRequest != null)
        {
            return Ok(translationRequest);
        }

        return NotFound(translationRequest);
    }

    /// <summary>
    /// Retries an existing translation request
    /// Does not delete the current one, just reques
    /// The request with the same information
    /// </summary>
    /// <param name="retryRequest">The translation request to retry</param>
    /// <response code="200">Returns the new translation request</response>
    /// <response code="404">If the translation request was not found</response>
    /// <response code="500">If there was an error checking for updates</response>
    /// <returns>ActionResult containing the new translation request if found, or NotFound if the request doesn't exist</returns>
    [HttpPost("retry")]
    public async Task<ActionResult<string>> RetryTranslationRequest([FromBody] TranslationRequest retryRequest)
    {
        var newTranslationRequest = await _translationRequestService.RetryTranslationRequest(retryRequest);
        if (newTranslationRequest != null)
        {
            return Ok(newTranslationRequest);
        }

        return NotFound(newTranslationRequest);
    }

    /// <summary>
    /// Resumes a Failed, Cancelled or Interrupted translation request, reusing
    /// any lines already translated by a previous attempt.
    /// </summary>
    /// <param name="resumeRequest">The translation request to resume</param>
    /// <response code="200">Returns a message describing the resumed request</response>
    /// <response code="404">If the translation request was not found or is not in a resumable state</response>
    /// <returns>ActionResult containing the resume result message</returns>
    [HttpPost("resume")]
    public async Task<ActionResult<string>> ResumeTranslationRequest([FromBody] TranslationRequest resumeRequest)
    {
        var result = await _translationRequestService.ResumeTranslationRequest(resumeRequest);
        if (result != null)
        {
            return Ok(result);
        }

        return NotFound(result);
    }
}