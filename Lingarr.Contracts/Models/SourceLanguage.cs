using System.Text.Json.Serialization;
using Lingarr.Contracts.Interfaces;

namespace Lingarr.Contracts.Models;

public class SourceLanguage : ILanguage
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("targets")]
    public List<string> Targets { get; set; } = [];
}
