using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace XIVSync.Interop;

public class UILoggingProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, UILogger> _loggers = new();
    private readonly ConcurrentQueue<LogEntry> _logEntries = new();
    private readonly int _maxEntries = 10000;

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new UILogger(name, _logEntries, _maxEntries));
    }

    public IEnumerable<LogEntry> GetLogEntries()
    {
        return _logEntries.ToArray();
    }

    public IEnumerable<LogEntry> GetRecentLogs(int count = 1000)
    {
        return _logEntries.TakeLast(count).ToArray();
    }

    public void ClearLogs()
    {
        _logEntries.Clear();
    }

    public void Dispose()
    {
        _loggers.Clear();
        _logEntries.Clear();
    }

    private class UILogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConcurrentQueue<LogEntry> _logEntries;
        private readonly int _maxEntries;

        public UILogger(string categoryName, ConcurrentQueue<LogEntry> logEntries, int maxEntries)
        {
            _categoryName = categoryName;
            _logEntries = logEntries;
            _maxEntries = maxEntries;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                LogLevel = logLevel,
                Category = _categoryName,
                Message = message,
                Exception = exception
            };

            _logEntries.Enqueue(logEntry);

            // Keep only the most recent entries
            while (_logEntries.Count > _maxEntries)
            {
                _logEntries.TryDequeue(out _);
            }
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel LogLevel { get; set; }
    public LogLevel Level => LogLevel; // Alias for compatibility
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}
