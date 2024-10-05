using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lingarr.Core.Logging;

public class CustomLogFormatter : ILoggerProvider
{
    private readonly CustomLogFormatterOptions _options;

    public CustomLogFormatter(IOptions<CustomLogFormatterOptions> options)
    {
        _options = options.Value;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ColoredConsoleLogger(categoryName, _options);
    }

    public void Dispose() { }
}