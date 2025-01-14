using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Lingarr.Core.Entities;

public class Statistics : BaseEntity
{
    public long TotalLinesTranslated { get; set; }
    public long TotalFilesTranslated { get; set; }
    public long TotalCharactersTranslated { get; set; }
    public int TotalMovies { get; set; }
    public int TotalEpisodes { get; set; }
    public int TotalSubtitles { get; set; }

    public string TranslationsByMediaTypeJson { get; set; } = "{}";
    public string TranslationsByServiceJson { get; set; } = "{}";
    public string SubtitlesByLanguageJson { get; set; } = "{}";

    [NotMapped]
    public Dictionary<string, int> TranslationsByMediaType
    {
        get => JsonSerializer.Deserialize<Dictionary<string, int>>(TranslationsByMediaTypeJson) ?? new();
        set => TranslationsByMediaTypeJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, int> TranslationsByService
    {
        get => JsonSerializer.Deserialize<Dictionary<string, int>>(TranslationsByServiceJson) ?? new();
        set => TranslationsByServiceJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, int> SubtitlesByLanguage
    {
        get => JsonSerializer.Deserialize<Dictionary<string, int>>(SubtitlesByLanguageJson) ?? new();
        set => SubtitlesByLanguageJson = JsonSerializer.Serialize(value);
    }
}