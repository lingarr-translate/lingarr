using Lingarr.Contracts.Exceptions;

namespace Lingarr.Server.Exceptions;

/// <summary>
/// Raised when a translation provider returns a response that cannot be parsed into translated
/// subtitles. AI models sample their output, so the same request usually succeeds when retried,
/// which makes this failure retryable unlike a configuration or transport error.
/// </summary>
public class TranslationParseException : TranslationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exception">The exception that caused the parse failure, if any.</param>
    public TranslationParseException(string message, Exception? exception = null) : base(message, exception)
    {
    }
}
