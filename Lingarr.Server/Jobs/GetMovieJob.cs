using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;

namespace Lingarr.Server.Jobs;

public class GetMovieJob
{
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

    public async Task Execute()
    {
        Console.WriteLine($"Radarr job initiated");
        try
        {
            var movies = await _radarrService.GetMovies();
            if (movies == null) return;
            
            _logger.LogInformation("Fetched {count} movies from Radarr", movies.Count());

            foreach (var movie in movies)
            {
                bool movieExists = _dbContext.Movies.Any(m => m.RadarrId == movie.id);
                if (movieExists)
                {
                    _logger.LogInformation($"Movie '{movie.title}' (ID: {movie.id}) already exists, skipping.");
                    continue;
                }

                if (!movie.hasFile)
                {
                    _logger.LogInformation($"Movie '{movie.title}' (ID: {movie.id}) already exists, skipping.");
                    continue;
                }

                var movieEntity = new Movie()
                {
                    RadarrId = movie.id,
                    Title = movie.title,
                    DateAdded = DateTime.Parse(movie.added),
                    FileName = Path.GetFileNameWithoutExtension(movie.movieFile?.path ?? string.Empty),
                    Path = Path.GetDirectoryName(movie.movieFile?.path) ?? string.Empty
                };

                _dbContext.Movies.Add(movieEntity);

                foreach (var image in movie.images)
                {
                    if (string.IsNullOrEmpty(image.coverType) || string.IsNullOrEmpty(image.url))
                    {
                        continue;
                    }

                    var imageEntity = new Media()
                    {
                        Type = image.coverType,
                        Path = image.url.Split('?')[0]
                    };

                    _dbContext.Media.Add(imageEntity);
                    movieEntity.Media.Add(imageEntity);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Movies processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when processing movies");
        }
    }
}