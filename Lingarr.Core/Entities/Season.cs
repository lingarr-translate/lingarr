using System.ComponentModel.DataAnnotations.Schema;

namespace Lingarr.Core.Entities;

public class Season : BaseEntity
{
    public required int SeasonNumber { get; set; }
    public string? Path { get; set; } = string.Empty;
    public List<Episode> Episodes { get; set; } = new();
    
    public int ShowId { get; set; }
    [ForeignKey(nameof(ShowId))]
    public required Show Show { get; set; }
    public bool ExcludeFromTranslation { get; set; }
}