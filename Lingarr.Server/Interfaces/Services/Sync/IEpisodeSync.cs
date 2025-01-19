using Lingarr.Core.Entities;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Sync;

public interface IEpisodeSync
{
    /// <summary>
    /// Synchronizes episodes for a given show and season
    /// </summary>
    /// <param name="show">The Sonarr show containing the episodes</param>
    /// <param name="season">The season to sync episodes for</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SyncEpisodes(SonarrShow show, Season season);
}