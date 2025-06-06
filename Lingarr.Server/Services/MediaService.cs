﻿using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Core.Enum;
using Lingarr.Server.Models;
using Microsoft.EntityFrameworkCore;
using Lingarr.Server.Models.Api;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Services;

public class MediaService : IMediaService
{
    private readonly LingarrDbContext _dbContext;
    private readonly ISubtitleService _subtitleService;
    private readonly ILogger<MediaService> _logger;

    public MediaService(LingarrDbContext dbContext, 
        ISubtitleService subtitleService,
        ILogger<MediaService> logger)
    {
        _dbContext = dbContext;
        _subtitleService = subtitleService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<PagedResult<MovieResponse>> GetMovies(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.Movies
            .Include(m => m.Images)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(m => m.Title.ToLower().Contains(searchQuery.ToLower()));
        }

        query = orderBy switch
        {
            "Id" => ascending ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id),
            "Title" => ascending ? query.OrderBy(m => m.Title) : query.OrderByDescending(m => m.Title),
            "DateAdded" => ascending ? query.OrderBy(m => m.DateAdded) : query.OrderByDescending(m => m.DateAdded),
            _ => ascending ? query.OrderBy(m => m.Title) : query.OrderByDescending(m => m.Title)
        };

        var totalCount = await query.CountAsync();
        var movies = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var enrichedMovies = new List<MovieResponse>();
        foreach (var movie in movies)
        {
            if (movie.Path == null)
            {
                continue;
            }

            var subtitles = await _subtitleService.GetAllSubtitles(movie.Path);
            var enrichedMovie = new MovieResponse
            {
                Id = movie.Id,
                RadarrId = movie.RadarrId,
                Title = movie.Title,
                FileName = movie.FileName ?? string.Empty,
                Path = movie.Path,
                DateAdded = movie.DateAdded,
                Images = movie.Images,
                Subtitles = subtitles,
                ExcludeFromTranslation = movie.ExcludeFromTranslation,
                TranslationAgeThreshold = movie.TranslationAgeThreshold
            };
            enrichedMovies.Add(enrichedMovie);
        }

        return new PagedResult<MovieResponse>
        {
            Items = enrichedMovies,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    
    /// <inheritdoc />
    public async Task<PagedResult<Show>> GetShows(
        string? searchQuery,
        string? orderBy,
        bool ascending,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.Shows
            .Include(s => s.Images)
            .Include(s => s.Seasons)
            .ThenInclude(season => season.Episodes)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(s => s.Title.ToLower().Contains(searchQuery.ToLower()));
        }

        query = orderBy switch
        {
            "Id" => ascending ? query.OrderBy(s => s.Id) : query.OrderByDescending(s => s.Id),
            "Title" => ascending ? query.OrderBy(s => s.Title) : query.OrderByDescending(s => s.Title),
            "DateAdded" => ascending ? query.OrderBy(s => s.DateAdded) : query.OrderByDescending(s => s.DateAdded),
            _ => ascending ? query.OrderBy(s => s.Title) : query.OrderByDescending(s => s.Title)
        };

        var totalCount = await query.CountAsync();
        var shows = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Show>
        {
            Items = shows,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    
    /// <inheritdoc />
    public async Task<bool> Exclude(
        MediaType mediaType,
        int id)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    var movie = await _dbContext.Movies.FindAsync(id);
                    if (movie != null)
                    {
                        movie.ExcludeFromTranslation = !movie.ExcludeFromTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Show:
                    var show = await _dbContext.Shows.FindAsync(id);
                    if (show != null)
                    {
                        show.ExcludeFromTranslation = !show.ExcludeFromTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Season:
                    var season = await _dbContext.Seasons.FindAsync(id);
                    if (season != null)
                    {
                        season.ExcludeFromTranslation = !season.ExcludeFromTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Episode:
                    var episode = await _dbContext.Episodes.FindAsync(id);
                    if (episode != null)
                    {
                        episode.ExcludeFromTranslation = !episode.ExcludeFromTranslation;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                default:
                    _logger.LogWarning("Unsupported media type: {MediaType}", mediaType);
                    return false;
            }

            _logger.LogWarning("Media item not found. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error excluding media item. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> Threshold(
        MediaType mediaType,
        int id,
        int hours)
    {
        try
        {
            switch (mediaType)
            {
                case MediaType.Movie:
                    var movie = await _dbContext.Movies.FindAsync(id);
                    if (movie != null)
                    {
                        movie.TranslationAgeThreshold = hours;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;

                case MediaType.Show:
                    var show = await _dbContext.Shows.FindAsync(id);
                    if (show != null)
                    {
                        show.TranslationAgeThreshold = hours;
                        await _dbContext.SaveChangesAsync();
                        return true;
                    }
                    break;
                
                default:
                    _logger.LogWarning("Unsupported media type: {MediaType}", mediaType);
                    return false;
            }

            _logger.LogWarning("Media item not found. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error excluding media item. Type: {MediaType}, Id: {Id}", mediaType, id);
            return false;
        }
    }
}