using Hangfire;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Filters;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Integration;
using Lingarr.Server.Models.Integrations;
using Lingarr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Jobs;

public class GetMovieJob
{
    private readonly LingarrDbContext _dbContext;
    private readonly IRadarrService _radarrService;
    private readonly ILogger<GetMovieJob> _logger;
    private readonly IScheduleService _scheduleService;
    private readonly PathConversionService _pathConversionService;

    public GetMovieJob(LingarrDbContext dbContext, 
        IRadarrService radarrService, 
        ILogger<GetMovieJob> logger, 
        IScheduleService scheduleService,
        PathConversionService pathConversionService)
    {
        _dbContext = dbContext;
        _radarrService = radarrService;
        _logger = logger;
        _scheduleService = scheduleService;
        _pathConversionService = pathConversionService;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
    [AutomaticRetry(Attempts = 0)]
    public async Task Execute()
    {
        var jobName = JobContextFilter.GetCurrentJobTypeName();
        _logger.LogInformation("Radarr job initiated");
        
        try
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Processing.GetDisplayName());

            var movies = await _radarrService.GetMovies();
            if (movies == null) return;
            
            _logger.LogInformation("Fetched {count} movies from Radarr", movies.Count());

            foreach (var movie in movies)
            {
                await CreateOrUpdateMovie(movie);
            }

            // Remove movies that no longer exist in Radarr
            var radarrIds = movies.Select(radarrMovie => radarrMovie.Id).ToList();
            var moviesToRemove = await _dbContext.Movies
                .Where(movie => !radarrIds.Contains(movie.RadarrId))
                .ToListAsync();
            
            _dbContext.Movies.RemoveRange(moviesToRemove);

            await _dbContext.SaveChangesAsync();
            await _scheduleService.UpdateJobState(jobName, JobStatus.Succeeded.GetDisplayName());
            _logger.LogInformation("Movies processed successfully.");
        }
        catch (Exception ex)
        {
            await _scheduleService.UpdateJobState(jobName, JobStatus.Failed.GetDisplayName());
            _logger.LogError(ex, "An error occurred when processing movies");
        }
    }

    private async Task CreateOrUpdateMovie(RadarrMovie movie)
    {
        if (!movie.HasFile)
        {
            _logger.LogDebug($"Movie '{movie.Title}' (ID: {movie.Id}) has no file, skipping.");
            return;
        }

        var movieEntity = await _dbContext.Movies
            .Include(m => m.Images)
            .FirstOrDefaultAsync(m => m.RadarrId == movie.Id);

        string moviePath = _pathConversionService.ConvertAndMapPath(
            movie.MovieFile.Path ?? string.Empty,
            MediaType.Movie
        );
        if (movieEntity == null)
        {
            movieEntity = new Movie
            {
                RadarrId = movie.Id,
                Title = movie.Title,
                DateAdded = DateTime.Parse(movie.Added),
                FileName = Path.GetFileNameWithoutExtension(moviePath),
                Path = Path.GetDirectoryName(moviePath) ?? string.Empty
            };
            _dbContext.Movies.Add(movieEntity);
        }
        else
        {
            movieEntity.Title = movie.Title;
            movieEntity.DateAdded = DateTime.Parse(movie.Added);
            movieEntity.FileName = Path.GetFileNameWithoutExtension(moviePath);
            movieEntity.Path = Path.GetDirectoryName(moviePath) ?? string.Empty;
        }
        _logger.LogInformation("Processing movie: {movieId} with Path: |Green|{Path}|/Green|", movie.Id, movieEntity.Path);

        if (movie.Images?.Any() == true)
        {
            ProcessImages(movieEntity, movie.Images);
        }
    }

    private void ProcessImages(Movie movieEntity, List<IntegrationImage> images)
    {
        var existingImageTypes = movieEntity.Images.Select(m => m.Type).ToHashSet();

        foreach (var image in images)
        {
            if (string.IsNullOrEmpty(image.CoverType) || string.IsNullOrEmpty(image.Url))
            {
                continue;
            }

            var imageUrl = image.Url.Split('?')[0];

            if (existingImageTypes.Contains(image.CoverType))
            {
                var existingImage = movieEntity.Images.First(m => m.Type == image.CoverType);
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
}