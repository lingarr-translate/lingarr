namespace Lingarr.Server.Interfaces.Services.Translation;

public interface ITranslationServiceFactory
{
    ITranslationService CreateTranslationService(string serviceType);
}