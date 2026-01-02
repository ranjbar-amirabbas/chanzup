using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Chanzup.Infrastructure.Logging;

public class StructuredLogger : ILogger
{
    private readonly ILogger _logger;
    private readonly string _categoryName;

    public StructuredLogger(ILogger logger, string categoryName)
    {
        _logger = logger;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Category = _categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = formatter(state, exception),
            Exception = exception?.ToString(),
            Properties = ExtractProperties(state)
        };

        var jsonLog = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.Log(logLevel, eventId, jsonLog, exception, (s, ex) => s);
    }

    private static Dictionary<string, object?> ExtractProperties<TState>(TState state)
    {
        var properties = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object?>> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Key != "{OriginalFormat}")
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }
        }

        return properties;
    }
}

public class StructuredLoggerProvider : ILoggerProvider
{
    private readonly ILoggerProvider _innerProvider;

    public StructuredLoggerProvider(ILoggerProvider innerProvider)
    {
        _innerProvider = innerProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = _innerProvider.CreateLogger(categoryName);
        return new StructuredLogger(innerLogger, categoryName);
    }

    public void Dispose()
    {
        _innerProvider.Dispose();
    }
}