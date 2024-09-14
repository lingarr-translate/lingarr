using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for interacting with the Radarr API.
/// </summary>
public interface IRadarrService
{
    /// <summary>
    /// Asynchronously retrieves a list of movies from the Radarr API.
    /// </summary>
    /// <returns>
    /// This method calls the Radarr API to fetch movies using an internal helper method <see cref="GetApiResponse{T}"/>.
    /// The task result contains a <see cref="List{T}"/> of <see cref="RadarrMovie"/>
    /// objects representing all movies, or <c>null</c> if the API call fails.
    /// </returns>
    Task<List<RadarrMovie>?> GetMovies();
}