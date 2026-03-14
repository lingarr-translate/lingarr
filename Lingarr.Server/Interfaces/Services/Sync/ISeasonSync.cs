using Lingarr.Core.Entities;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Sync;

public interface ISeasonSync
{
    /// <summary>
    /// Synchronizes a single season from Sonarr
    /// </summary>
    /// <param name="show">The show entity the season belongs to</param>
    /// <param name="sonarrShow">The Sonarr show containing the season</param>
    /// <param name="season">The Sonarr season to sync</param>
    /// <param name="defaultInclude">Whether newly imported seasons should be included in translation by default</param>
    /// <returns>The synchronized season entity</returns>
    Task<Season> SyncSeason(Show show, SonarrShow sonarrShow, SonarrSeason season, bool defaultInclude = true);
}