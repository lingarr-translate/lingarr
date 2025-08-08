using Lingarr.Core.Entities;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Sync;

public interface IMovieSyncService
{
    /// <summary>
    /// Synchronizes multiple movies from Radarr
    /// </summary>
    /// <param name="movies">The list of Radarr movies to sync</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SyncMovies(List<RadarrMovie> movies);

    /// <summary>
    /// Synchronizes a movie from Radarr
    /// </summary>
    /// <param name="movie">The Radarr movie to sync</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task<Movie?> SyncMovie(RadarrMovie movie);

    /// <summary>
    /// Removes movies that no longer exist in Radarr
    /// </summary>
    /// <param name="existingRadarrIds">The collection of currently existing Radarr movie IDs</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RemoveNonExistentMovies(IEnumerable<int> existingRadarrIds);
}