
namespace Lingarr.Core.Entities;

public class TranslationJob : BaseEntity
{
    public required string JobId  { get; set; }
    public required bool Completed  { get; set; }

}