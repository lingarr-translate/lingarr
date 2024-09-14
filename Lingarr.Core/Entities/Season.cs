using System.ComponentModel.DataAnnotations.Schema;

namespace Lingarr.Core.Entities;

public class Season : BaseEntity
{
    public required int SeasonNumber { get; set; }
    public required string Path { get; set; }
    public List<Episode> Episodes { get; set; } = new();
    
    public int ShowId { get; set; }
    [ForeignKey(nameof(ShowId))]
    public required Show Show { get; set; }
}