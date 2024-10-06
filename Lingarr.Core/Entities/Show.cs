namespace Lingarr.Core.Entities;

public class Show : BaseEntity
{
    public required int SonarrId { get; set; }
    public required string Title { get; set; }
    public required string Path { get; set; }
    public required DateTime? DateAdded { get; set; }
    public List<Image> Images { get; set; } = new();
    public List<Season> Seasons { get; set; } = new();
}