using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.Api;

public class BulkTranslateRequest
{
    public required List<int> MediaIds { get; set; }
    public required string TargetLanguage { get; set; }
    public required MediaType MediaType { get; set; }
}
