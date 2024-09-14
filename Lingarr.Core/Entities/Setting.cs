using System.ComponentModel.DataAnnotations;

namespace Lingarr.Core.Entities;

public class Setting
{
    [Key]
    [MaxLength(255)]
    public required string Key { get; set; }
    public required string Value { get; set; }
}