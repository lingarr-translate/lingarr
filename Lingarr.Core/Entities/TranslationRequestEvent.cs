using Lingarr.Core.Enum;

namespace Lingarr.Core.Entities;

public class TranslationRequestEvent : BaseEntity
{
    public int TranslationRequestId { get; set; }
    public TranslationRequest TranslationRequest { get; set; } = null!;
    public required TranslationStatus Status { get; set; }
    public string? Message { get; set; }
}
