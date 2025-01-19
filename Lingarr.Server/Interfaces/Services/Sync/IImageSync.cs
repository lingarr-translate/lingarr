using Lingarr.Core.Entities;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Interfaces.Services.Sync;

public interface IImageSync
{
    /// <summary>
    /// Synchronizes images between source and entity
    /// </summary>
    /// <param name="entityImages">The list of existing entity images</param>
    /// <param name="sourceImages">The list of source images to sync from</param>
    void SyncImages(List<Image> entityImages, List<IntegrationImage>? sourceImages);
}