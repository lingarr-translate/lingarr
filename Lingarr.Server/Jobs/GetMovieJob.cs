using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Jobs;

public class GetMovieJob
{
    private const string LingarRootFolder = "/movies/";
    
    private readonly LingarrDbContext _dbContext;
    private readonly IRadarrService _radarrService;
    private readonly ILogger<GetMovieJob> _logger;

    public GetMovieJob(LingarrDbContext dbContext, 
        IRadarrService radarrService, 
        ILogger<GetMovieJob> logger)
    {
        _dbContext = dbContext;
        _radarrService = radarrService;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task Execute(IJobCancellationToken cancellationToken)
    {
        _logger.LogInformation("Radarr job initiated");
        try
        {
            var movies = await _radarrService.GetMovies();
            if (movies == null) return;
            
            _logger.LogInformation("Fetched {count} movies from Radarr", movies.Count());

            foreach (var movie in movies)
            {
                await CreateOrUpdateMovie(movie);
            }

            // Remove movies that no longer exist in Radarr
            var radarrIds = movies.Select(radarrMovie => radarrMovie.id).ToList();
            var moviesToRemove = await _dbContext.Movies
                .Where(movie => !radarrIds.Contains(movie.RadarrId))
                .ToListAsync();
            
            _dbContext.Movies.RemoveRange(moviesToRemove);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Movies processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when processing movies");
        }
    }

    private async Task CreateOrUpdateMovie(RadarrMovie movie)
    {
        if (!movie.hasFile)
        {
            _logger.LogInformation($"Movie '{movie.title}' (ID: {movie.id}) has no file, skipping.");
            return;
        }

        var movieEntity = await _dbContext.Movies
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.RadarrId == movie.id);

        string moviePath = GetPath(movie);
        if (movieEntity == null)
        {
            movieEntity = new Movie
            {
                RadarrId = movie.id,
                Title = movie.title,
                DateAdded = DateTime.Parse(movie.added),
                FileName = Path.GetFileNameWithoutExtension(moviePath),
                Path = Path.GetDirectoryName(moviePath) ?? string.Empty
            };
            _dbContext.Movies.Add(movieEntity);
            _logger.LogInformation($"Created new movie '{movie.title}' (ID: {movie.id}).");
        }
        else
        {
            movieEntity.Title = movie.title;
            movieEntity.DateAdded = DateTime.Parse(movie.added);
            movieEntity.FileName = Path.GetFileNameWithoutExtension(moviePath);
            movieEntity.Path = Path.GetDirectoryName(moviePath) ?? string.Empty;
        }
        
        ProcessImages(movieEntity, movie.images);
    }

    private void ProcessImages(Movie movieEntity, List<IntegrationImage> images)
    {
        var existingImageTypes = movieEntity.Images.Select(m => m.Type).ToHashSet();

        foreach (var image in images)
        {
            if (string.IsNullOrEmpty(image.coverType) || string.IsNullOrEmpty(image.url))
            {
                continue;
            }

            var imageUrl = image.url.Split('?')[0];

            if (existingImageTypes.Contains(image.coverType))
            {
                var existingImage = movieEntity.Images.First(m => m.Type == image.coverType);
                existingImage.Path = imageUrl;
                existingImageTypes.Remove(image.coverType);
            }
            else
            {
                var newImage = new Image
                {
                    Type = image.coverType,
                    Path = imageUrl
                };
                _dbContext.Images.Add(newImage);
                movieEntity.Images.Add(newImage);
            }
        }

        // Remove images that no longer exist
        var imagesToRemove = movieEntity.Images.Where(m => existingImageTypes.Contains(m.Type)).ToList();
        foreach (var imageToRemove in imagesToRemove)
        {
            movieEntity.Images.Remove(imageToRemove);
            _dbContext.Images.Remove(imageToRemove);
        }
    }

    private string GetPath(RadarrMovie movie)
    {
        if (movie.movieFile.path != null &&
            movie.movieFile.path.StartsWith(movie.rootFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return LingarRootFolder + movie.movieFile.path.Substring(movie.rootFolderPath.Length);
        }

        return movie.movieFile.path ?? string.Empty;
    }
}