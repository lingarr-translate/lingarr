using Lingarr.Core.Entities;
using Lingarr.Core.Enum;

namespace Lingarr.Server.Interfaces.Services;

public interface ITranslationRequestEventService
{
    Task LogEvent(int translationRequestId, TranslationStatus status, string? message = null);
    Task<List<TranslationRequestEvent>> GetEvents(int translationRequestId);
}
