using System.ComponentModel.DataAnnotations;

namespace Lingarr.Core.Entities;

public abstract class BaseEntity
{
    [Key] 
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}