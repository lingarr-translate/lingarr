﻿using Lingarr.Core.Entities;
using Lingarr.Server.Models;
using Lingarr.Server.Models.Api;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for retrieving and managing media information.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Retrieves a paginated and optionally filtered and ordered list of movies asynchronously.
    /// </summary>
    /// <param name="searchQuery">An optional search query to filter movies by title. If null or empty, no filtering is applied.</param>
    /// <param name="orderBy">An optional field to order the results by. Possible values include "Id", "Title", and "DateAdded".</param>
    /// <param name="ascending">Specifies whether the sorting should be in ascending order. If false, results are sorted in descending order.</param>
    /// <param name="pageNumber">The number of the page to retrieve. Must be greater than or equal to 1.</param>
    /// <param name="pageSize">The number of items per page. Must be greater than or equal to 1.</param>
    /// <returns>
    /// A task result containing a <see cref="PagedResult{MovieResponse}"/> a list of movies, 
    /// along with the total count of movies that match the criteria.
    /// </returns>
    Task<PagedResult<MovieResponse>> GetMovies(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Retrieves a paginated and optionally filtered and ordered list of shows asynchronously.
    /// </summary>
    /// <param name="searchQuery">An optional search query to filter shows by title. If null or empty, no filtering is applied.</param>
    /// <param name="orderBy">An optional field to order the results by. Possible values include "Id", "Title", and "DateAdded".</param>
    /// <param name="ascending">Specifies whether the sorting should be in ascending order. If false, results are sorted in descending order.</param>
    /// <param name="pageNumber">The number of the page to retrieve. Must be greater than or equal to 1.</param>
    /// <param name="pageSize">The number of items per page. Must be greater than or equal to 1.</param>
    /// <returns>
    /// A task result containing a <see cref="PagedResult{Show}"/> a list of shows, 
    /// along with the total count of shows that match the criteria.
    /// </returns>
    Task<PagedResult<Show>> GetShows(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize);
}