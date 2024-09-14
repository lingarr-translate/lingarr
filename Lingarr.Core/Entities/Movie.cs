namespace Lingarr.Core.Entities;

public class Movie : BaseEntity
{
    public required int RadarrId { get; set; }
    public required string Title { get; set; }
    public required string FileName { get; set; }
    public required string Path { get; set; }
    public required DateTime? DateAdded { get; set; }
    public List<Media> Media { get; set; } = new();
}