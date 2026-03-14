using Lingarr.Core.Entities;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Sync;

public interface IMovieSync
{
    /// <summary>
    /// Synchronizes a single movie from Radarr
    /// </summary>
    /// <param name="movie">The Radarr movie to sync</param>
    /// <param name="defaultInclude">Whether newly imported movies should be included in translation by default</param>
    /// <returns>The synchronized movie entity or null if the movie has no file</returns>
    Task<Movie?> SyncMovie(RadarrMovie movie, bool defaultInclude = true);
}
