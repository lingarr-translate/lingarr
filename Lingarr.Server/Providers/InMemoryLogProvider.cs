using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Lingarr.Server.Providers
{
    public class LogEntry
    {
        private LogLevel _logLevel;

        public LogLevel LogLevel
        {
            get => _logLevel;
            set => _logLevel = value;
        }

        [JsonPropertyName("logLevel")] 
        public string LogLevelString => LogLevel.ToString();
        
        [JsonPropertyName("message")] 
        public string? Message { get; set; }
        
        [JsonPropertyName("timestamp")] 
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("category")] 
        public string? Category { get; set; }

        [JsonPropertyName("formattedTime")] 
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
        
        [JsonPropertyName("formattedDate")] 
        public string FormattedDate => Timestamp.ToString("yyyy-MM-dd");

        [JsonPropertyName("formattedSource")]
        public string FormattedSource => Category?.Split('.').LastOrDefault() ?? Category ?? "";
    }

    public static class InMemoryLogSink
    {
        private static readonly ConcurrentQueue<LogEntry> Logs = new();
        private static readonly int MaxLogCount = 1000;

        public static void AddLog(LogEntry logEntry)
        {
            Logs.Enqueue(logEntry);

            // Trim logs if we exceed the maximum count and only keep logs for 24h
            while (Logs.Count > MaxLogCount && Logs.TryDequeue(out _))
            {
            }
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            while (Logs.TryPeek(out var oldestLog) && oldestLog.Timestamp < cutoffTime && Logs.TryDequeue(out _))
            {
            }
        }

        public static IEnumerable<LogEntry> GetRecentLogs(int count)
        {
            return Logs.TakeLast(count);
        }

        public static ConcurrentQueue<LogEntry> LogQueue => Logs;
    }

    public class InMemoryLogger : ILogger
    {
        private readonly string _categoryName;

        public InMemoryLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (exception != null)
                message += $"\nException: {exception}";

            var logEntry = new LogEntry
            {
                LogLevel = logLevel,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Category = _categoryName
            };

            InMemoryLogSink.AddLog(logEntry);
        }
    }

    public class InMemoryLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new InMemoryLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }
}