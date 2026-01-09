namespace Lingarr.Server.Models.Api;

public class IncludeSummaryResponse
{
    public int TotalMovies { get; set; }
    public int ExcludedMovies { get; set; }
    public int TotalShows { get; set; }
    public int ExcludedShows { get; set; }
}
