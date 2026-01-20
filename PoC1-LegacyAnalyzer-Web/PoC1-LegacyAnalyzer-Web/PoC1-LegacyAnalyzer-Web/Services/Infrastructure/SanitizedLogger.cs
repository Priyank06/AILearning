using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Services;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Wrapper for ILogger that automatically sanitizes log messages.
    /// </summary>
    public class SanitizedLogger<T> : ILogger<T>
    {
        private readonly ILogger<T> _innerLogger;
        private readonly ILogSanitizationService _sanitizationService;

        public SanitizedLogger(ILogger<T> innerLogger, ILogSanitizationService sanitizationService)
        {
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
            _sanitizationService = sanitizationService ?? throw new ArgumentNullException(nameof(sanitizationService));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _innerLogger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _innerLogger.IsEnabled(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            // Sanitize the formatted message
            var originalMessage = formatter(state, exception);
            var sanitizedMessage = _sanitizationService.SanitizeMessage(originalMessage);

            // Sanitize exception if present
            Exception? sanitizedException = null;
            if (exception != null)
            {
                // Create a sanitized exception wrapper
                var sanitizedExceptionMessage = _sanitizationService.SanitizeException(exception);
                sanitizedException = new Exception(sanitizedExceptionMessage);
            }

            // Log with sanitized content
            _innerLogger.Log(logLevel, eventId, state, sanitizedException, (s, e) => sanitizedMessage);
        }
    }
}

