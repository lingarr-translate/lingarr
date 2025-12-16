using System.ComponentModel.DataAnnotations;

namespace Lingarr.Core.Entities;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Username { get; set; }

    [Required]
    [MaxLength(255)]
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
