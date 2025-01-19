using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services.Sync;

public class ShowSync : IShowSync
{
    private readonly LingarrDbContext _dbContext;
    private readonly IImageSync _imageSync;

    public ShowSync(
        LingarrDbContext dbContext,
        IImageSync imageSync)
    {
        _dbContext = dbContext;
        _imageSync = imageSync;
    }

    /// <inheritdoc />
    public async Task<Show> SyncShow(SonarrShow sonarrShow)
    {
        var showEntity = await _dbContext.Shows
            .Include(s => s.Images)
            .Include(s => s.Seasons)
            .FirstOrDefaultAsync(s => s.SonarrId == sonarrShow.Id);

        if (showEntity == null)
        {
            showEntity = new Show
            {
                SonarrId = sonarrShow.Id,
                Title = sonarrShow.Title,
                Path = sonarrShow.Path,
                DateAdded = !string.IsNullOrEmpty(sonarrShow.Added) ? DateTime.Parse(sonarrShow.Added) : DateTime.UtcNow
            };
            _dbContext.Shows.Add(showEntity);
        }
        else
        {
            showEntity.Title = sonarrShow.Title;
            showEntity.Path = sonarrShow.Path;
            showEntity.DateAdded = !string.IsNullOrEmpty(sonarrShow.Added) ? DateTime.Parse(sonarrShow.Added) : DateTime.UtcNow;
        }

        if (sonarrShow.Images?.Any() == true)
        {
            _imageSync.SyncImages(showEntity.Images, sonarrShow.Images);
        }

        return showEntity;
    }
}
