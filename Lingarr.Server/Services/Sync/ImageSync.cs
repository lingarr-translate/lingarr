using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Models.Integrations;

namespace Lingarr.Server.Services.Sync;

public class ImageSync : IImageSync
{
    private readonly LingarrDbContext _dbContext;

    public ImageSync(LingarrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public void SyncImages(List<Image> entityImages, List<IntegrationImage>? sourceImages)
    {
        if (sourceImages == null || !sourceImages.Any())
        {
            return;
        }

        var existingImageTypes = entityImages.Select(m => m.Type).ToHashSet();

        foreach (var image in sourceImages)
        {
            if (string.IsNullOrEmpty(image.CoverType) || string.IsNullOrEmpty(image.Url))
            {
                continue;
            }

            var imageUrl = image.Url.Split('?')[0];

            if (existingImageTypes.Contains(image.CoverType))
            {
                var existingImage = entityImages.First(m => m.Type == image.CoverType);
                existingImage.Path = imageUrl;
                existingImageTypes.Remove(image.CoverType);
            }
            else
            {
                var newImage = new Image
                {
                    Type = image.CoverType,
                    Path = imageUrl
                };
                _dbContext.Images.Add(newImage);
                entityImages.Add(newImage);
            }
        }

        // Remove images that no longer exist
        var imagesToRemove = entityImages.Where(m => existingImageTypes.Contains(m.Type)).ToList();
        foreach (var imageToRemove in imagesToRemove)
        {
            entityImages.Remove(imageToRemove);
            _dbContext.Images.Remove(imageToRemove);
        }
    }
}