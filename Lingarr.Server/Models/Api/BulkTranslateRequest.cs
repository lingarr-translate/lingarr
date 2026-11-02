using System.ComponentModel.DataAnnotations;
using Lingarr.Core.Enum;

namespace Lingarr.Server.Models.Api;

public class BulkTranslateRequest
{
    public List<int> MediaIds { get; set; } = [];
    public required string TargetLanguage { get; set; }
    [EnumDataType(typeof(MediaType))]
    public required MediaType MediaType { get; set; }

    /// <summary>
    /// When true, translate every movie or show matching <see cref="SearchQuery"/>,
    /// except <see cref="ExcludeIds"/>. <see cref="MediaIds"/> is ignored.
    /// </summary>
    public bool AllMatching { get; set; }
    public string? SearchQuery { get; set; }
    public List<int> ExcludeIds { get; set; } = [];
}
