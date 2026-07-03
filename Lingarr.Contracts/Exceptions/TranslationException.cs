namespace Lingarr.Contracts.Exceptions;

/// <summary>
/// Raised by translation providers when an operation fails
/// </summary>
public class TranslationException : Exception
{
    public TranslationException(string message, Exception? exception = null)
        : base(message, exception)
    {
    }
}
