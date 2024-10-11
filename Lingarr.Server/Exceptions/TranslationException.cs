namespace Lingarr.Server.Exceptions;

public class TranslationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exception"></param>
    public TranslationException(string message, Exception? exception = null) : base(message)
    {
    }
}
