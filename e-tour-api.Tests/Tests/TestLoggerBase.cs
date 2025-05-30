using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace e_tour_api.Tests.Tests
{
    public class TestLoggerBase
    {
        protected readonly List<LogMessage> LogMessages = new();

        protected class TestLogger<T> : ILogger<T>
        {
            private readonly List<LogMessage> _logMessages;

            public TestLogger(List<LogMessage> logMessages)
            {
                _logMessages = logMessages;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _logMessages.Add(new LogMessage
                {
                    LogLevel = logLevel,
                    Message = formatter(state, exception),
                    Exception = exception,
                    EventId = eventId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        protected class LogMessage
        {
            public LogLevel LogLevel { get; set; }
            public string Message { get; set; } = string.Empty;
            public Exception? Exception { get; set; }
            public EventId EventId { get; set; }
            public DateTime Timestamp { get; set; }
        }

        protected void AssertLogMessage(LogLevel expectedLevel, string expectedMessageContains)
        {
            Assert.Contains(LogMessages, log =>
                log.LogLevel == expectedLevel &&
                log.Message.Contains(expectedMessageContains));
        }

        protected void AssertLogMessage(LogLevel expectedLevel, string expectedMessageContains, Exception? expectedException)
        {
            Assert.Contains(LogMessages, log =>
                log.LogLevel == expectedLevel &&
                log.Message.Contains(expectedMessageContains) &&
                (expectedException == null || log.Exception?.Message == expectedException.Message));
        }

        protected void ClearLogMessages()
        {
            LogMessages.Clear();
        }
    }
} 