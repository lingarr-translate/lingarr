using Lingarr.Core.Entities;

namespace Lingarr.Server.Services;

/// <summary>
/// Title filter shared by the paged media and translation lists and by
/// selections that match every page of those lists.
/// </summary>
internal static class TitleSearch
{
    public static IQueryable<Movie> Apply(IQueryable<Movie> query, string? searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return query;
        }

        return query.Where(movie => movie.Title.ToLower().Contains(searchQuery.ToLower()));
    }

    public static IQueryable<Show> Apply(IQueryable<Show> query, string? searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return query;
        }

        return query.Where(show => show.Title.ToLower().Contains(searchQuery.ToLower()));
    }

    public static IQueryable<TranslationRequest> Apply(IQueryable<TranslationRequest> query, string? searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return query;
        }

        return query.Where(translationRequest =>
            translationRequest.Title.ToLower().Contains(searchQuery.ToLower()));
    }

    public static IQueryable<Movie> Exclude(IQueryable<Movie> query, IReadOnlyCollection<int> excludeIds)
    {
        if (excludeIds.Count == 0)
        {
            return query;
        }

        var excluded = excludeIds.ToList();
        return query.Where(movie => !excluded.Contains(movie.Id));
    }

    public static IQueryable<Show> Exclude(IQueryable<Show> query, IReadOnlyCollection<int> excludeIds)
    {
        if (excludeIds.Count == 0)
        {
            return query;
        }

        var excluded = excludeIds.ToList();
        return query.Where(show => !excluded.Contains(show.Id));
    }

    public static IQueryable<TranslationRequest> Exclude(
        IQueryable<TranslationRequest> query,
        IReadOnlyCollection<int> excludeIds)
    {
        if (excludeIds.Count == 0)
        {
            return query;
        }

        var excluded = excludeIds.ToList();
        return query.Where(translationRequest => !excluded.Contains(translationRequest.Id));
    }
}
