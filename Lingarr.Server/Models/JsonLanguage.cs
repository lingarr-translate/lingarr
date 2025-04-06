using System.Text.Json.Serialization;
using Lingarr.Server.Interfaces;

namespace Lingarr.Server.Models;

public class JsonLanguage : ILanguage
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}