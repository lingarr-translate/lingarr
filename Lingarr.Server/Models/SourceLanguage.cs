using System.Text.Json.Serialization;
using Lingarr.Server.Interfaces;

namespace Lingarr.Server.Models;

public class SourceLanguage : ILanguage
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("code")]
    public required string Code { get; set; }
    [JsonPropertyName("targets")]
    public required List<string> Targets { get; set; }
}