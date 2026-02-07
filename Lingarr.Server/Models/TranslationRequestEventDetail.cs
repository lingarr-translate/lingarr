using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class TranslationRequestEventDetail
{
    public int Id { get; set; }
    public required TranslationStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}