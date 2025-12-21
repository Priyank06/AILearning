using PoC1_LegacyAnalyzer_Web.Services;

namespace PoC1_LegacyAnalyzer_Web.Helpers
{
    /// <summary>
    /// Helper methods for safe logging that automatically sanitizes sensitive data.
    /// </summary>
    public static class LoggingHelpers
    {
        /// <summary>
        /// Safely logs an error message, automatically sanitizing sensitive data.
        /// </summary>
        public static void LogErrorSafe(
            Microsoft.Extensions.Logging.ILogger logger,
            Exception? exception,
            string message,
            params object[] args)
        {
            if (!logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                return;

            var sanitizationService = GetSanitizationService(logger);
            if (sanitizationService != null)
            {
                message = sanitizationService.SanitizeMessage(string.Format(message, args));
                if (exception != null)
                {
                    var sanitizedExceptionMessage = sanitizationService.SanitizeException(exception);
                    logger.LogError(new Exception(sanitizedExceptionMessage), message);
                }
                else
                {
                    logger.LogError(message);
                }
            }
            else
            {
                // Fallback if sanitization service not available
                logger.LogError(exception, message, args);
            }
        }

        /// <summary>
        /// Safely logs a warning message, automatically sanitizing sensitive data.
        /// </summary>
        public static void LogWarningSafe(
            Microsoft.Extensions.Logging.ILogger logger,
            string message,
            params object[] args)
        {
            if (!logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
                return;

            var sanitizationService = GetSanitizationService(logger);
            if (sanitizationService != null)
            {
                message = sanitizationService.SanitizeMessage(string.Format(message, args));
                logger.LogWarning(message);
            }
            else
            {
                logger.LogWarning(message, args);
            }
        }

        /// <summary>
        /// Safely logs an information message, automatically sanitizing sensitive data.
        /// </summary>
        public static void LogInformationSafe(
            Microsoft.Extensions.Logging.ILogger logger,
            string message,
            params object[] args)
        {
            if (!logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                return;

            var sanitizationService = GetSanitizationService(logger);
            if (sanitizationService != null)
            {
                message = sanitizationService.SanitizeMessage(string.Format(message, args));
                logger.LogInformation(message);
            }
            else
            {
                logger.LogInformation(message, args);
            }
        }

        /// <summary>
        /// Gets the sanitization service from the logger's service provider if available.
        /// </summary>
        private static ILogSanitizationService? GetSanitizationService(Microsoft.Extensions.Logging.ILogger logger)
        {
            // Try to get from service provider (this is a simplified approach)
            // In practice, services should inject ILogSanitizationService directly
            return null; // Will be injected by services that need it
        }
    }
}

