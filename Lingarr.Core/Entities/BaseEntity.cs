using System.ComponentModel.DataAnnotations;

namespace Lingarr.Core.Entities;

public abstract class BaseEntity
{
    [Key] 
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}