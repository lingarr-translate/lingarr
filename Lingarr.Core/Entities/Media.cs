using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lingarr.Core.Entities;

public class Media
{
    [Key] 
    public int Id { get; set; }
    public required string Type { get; set; }
    public required string Path { get; set; }

    public int? ShowId { get; set; }
    [ForeignKey(nameof(ShowId))]
    public Show? Show { get; set; }
    
    public int? MovieId { get; set; }
    [ForeignKey(nameof(MovieId))]
    public Movie? Movie { get; set; }
}