using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.TranslationRequests;

/// <summary>
/// A Pending or InProgress translation request, broadcast to clients so rows
/// can resolve their state by matching on MediaId and MediaType
/// </summary>
public class ActiveTranslation
{
    public int? MediaId { get; set; }
    public required MediaType MediaType { get; set; }
    public required TranslationStatus Status { get; set; }
}
