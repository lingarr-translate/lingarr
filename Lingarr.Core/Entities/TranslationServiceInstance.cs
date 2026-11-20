namespace Lingarr.Core.Entities;

public class TranslationServiceInstance : BaseEntity
{
    public required string Provider { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
    public List<TranslationServiceInstanceSetting> Settings { get; set; } = new();
}
