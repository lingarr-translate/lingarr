

using Microsoft.Extensions.Logging;

namespace Lingarr.Core.Logging;

public class ColoredConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly CustomLogFormatterOptions _options;

    public ColoredConsoleLogger(string categoryName, CustomLogFormatterOptions options)
    {
        _categoryName = categoryName;
        _options = options;
    }

    IDisposable? ILogger.BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        message = ApplyColorEmphasis(message);

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {_categoryName}: {message}");

        if (exception != null)
        {
            Console.WriteLine(exception.ToString());
        }
    }

    private string ApplyColorEmphasis(string message)
    {
        message = message.Replace("|Reset|", _options.Reset);
        message = message.Replace("|/Reset|", _options.Reset);
        message = message.Replace("|Green|", _options.Green);
        message = message.Replace("|/Green|", _options.Reset);
        message = message.Replace("|Orange|", _options.Orange);
        message = message.Replace("|/Orange|", _options.Reset);
        message = message.Replace("|Red|", _options.Red);
        message = message.Replace("|/Red|", _options.Reset);
        return message;
    }
}