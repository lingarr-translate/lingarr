using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.Api;

public class BulkRemoveRequest
{
    public List<int> Ids { get; set; } = [];
    public bool AllMatching { get; set; }
    public string? SearchQuery { get; set; }
    public List<TranslationStatus> Statuses { get; set; } = [];
    public List<int> ExcludeIds { get; set; } = [];
}
