using System.ComponentModel.DataAnnotations.Schema;
using Lingarr.Core.Interfaces;

namespace Lingarr.Core.Entities;

public class Episode : BaseEntity, IMedia
{
    public required int SonarrId { get; set; }
    public required int EpisodeNumber { get; set; }
    public required string Title { get; set; }
    public string? FileName { get; set; } = string.Empty;
    public string? Path { get; set; } = string.Empty;
    public string? MediaHash { get; set; } = string.Empty;
    public DateTime? DateAdded { get; set; }

    public int SeasonId { get; set; }
    [ForeignKey(nameof(SeasonId))]
    public required Season Season { get; set; }
    public bool ExcludeFromTranslation { get; set; }
}