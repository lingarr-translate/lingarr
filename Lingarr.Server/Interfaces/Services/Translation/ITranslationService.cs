namespace Lingarr.Server.Interfaces.Services.Translation;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
}