using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Sync;

public interface IShowSyncService
{
    /// <summary>
    /// Synchronizes multiple shows from Sonarr
    /// </summary>
    /// <param name="shows">The list of Sonarr shows to sync</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SyncShows(List<SonarrShow> shows);

    /// <summary>
    /// Removes shows that no longer exist in Sonarr
    /// </summary>
    /// <param name="existingSonarrIds">The set of currently existing Sonarr show IDs</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RemoveNonExistentShows(HashSet<int> existingSonarrIds);
}