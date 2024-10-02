using System.Text.Json.Serialization;
using Lingarr.Server.Interfaces;

namespace Lingarr.Server.Models;

public class TargetLanguage : ILanguage
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("code")]
    public required string Code { get; set; }
}