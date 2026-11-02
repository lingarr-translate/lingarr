using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Interfaces.Services.Sync;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class MediaServiceSelectionTests
{
    [Fact]
    public async Task GetMatchingMovieIds_FiltersByTitleAndExclusions()
    {
        await using var context = await CreateContext();
        context.Movies.AddRange(
            Movie(1, "Space Adventure"),
            Movie(2, "Space Opera"),
            Movie(3, "Garden Day"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var ids = await service.GetMatchingMovieIds("space", [2]);

        Assert.Equal([1], ids);
    }

    [Fact]
    public async Task GetMatchingShowIds_FiltersByTitleAndExclusions()
    {
        await using var context = await CreateContext();
        context.Shows.AddRange(
            Show(1, "Space Station"),
            Show(2, "Space Hospital"),
            Show(3, "Cooking"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var ids = await service.GetMatchingShowIds("space", [1]);

        Assert.Equal([2], ids);
    }

    private static async Task<LingarrDbContext> CreateContext()
    {
        var options = new DbContextOptionsBuilder<LingarrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new LingarrDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static MediaService CreateService(LingarrDbContext context)
    {
        return new MediaService(
            context,
            new Mock<ISubtitleService>().Object,
            new Mock<ISonarrService>().Object,
            new Mock<IShowSyncService>().Object,
            new Mock<IRadarrService>().Object,
            new Mock<IMovieSyncService>().Object,
            new Mock<ISettingService>().Object,
            NullLogger<MediaService>.Instance);
    }

    private static Movie Movie(int id, string title)
    {
        return new Movie
        {
            Id = id,
            RadarrId = id,
            Title = title,
            FileName = "file",
            Path = $"/movies/{id}",
            DateAdded = DateTimeOffset.UtcNow
        };
    }

    private static Show Show(int id, string title)
    {
        return new Show
        {
            Id = id,
            SonarrId = id,
            Title = title,
            Path = $"/tv/{id}",
            DateAdded = DateTimeOffset.UtcNow
        };
    }
}
