using System.ComponentModel.DataAnnotations;

namespace Lingarr.Core.Entities;

public class TranslationServiceInstanceSetting
{
    [Key]
    public int Id { get; set; }
    public int TranslationServiceInstanceId { get; set; }
    public TranslationServiceInstance TranslationServiceInstance { get; set; } = null!;
    public required string Key { get; set; }
    public required string Value { get; set; }
}
