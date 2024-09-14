using Lingarr.Core.Entities;
using Lingarr.Server.Models.FileSystem;

namespace Lingarr.Server.Models.Api;

public class MovieResponse
{
    public int Id { get; set; }
    public required int RadarrId { get; set; }
    public required string Title { get; set; }
    public required string FileName { get; set; }
    public required string Path { get; set; }
    public required DateTime? DateAdded { get; set; }
    public List<Media> Media { get; set; } = new();

    public List<Subtitles> Subtitles { get; set; } = new();
}