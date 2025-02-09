using Lingarr.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models;

namespace Lingarr.Server.Controllers;

[ApiController]
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
    /// Gets the count of active translation requests
    /// </summary>
    /// <response code="200">Returns the count of active translation requests</response>
    /// <response code="500">If there was an error checking for updates</response>
    /// <returns>ActionResult containing the count of active translation requests</returns>
    [HttpGet("active")]
    public async Task<ActionResult<int>> GetActiveTranslationCount()
    {
        var activeCount = await _translationRequestService.GetActiveCount();
        return Ok(activeCount);
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
}