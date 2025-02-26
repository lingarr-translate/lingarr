namespace Lingarr.Server.Exceptions;

public class TranslationResponseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationResponseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exception"></param>
    public TranslationResponseException(string message, Exception? exception = null) : base(message)
    {
    }
}
