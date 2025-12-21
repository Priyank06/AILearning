using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Services;

namespace PoC1_LegacyAnalyzer_Web.Extensions
{
    /// <summary>
    /// Extension methods for logging with sanitization.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Adds sanitized logging to the service collection.
        /// Registers the sanitization service and provides extension methods for getting sanitized loggers.
        /// </summary>
        public static IServiceCollection AddSanitizedLogging(this IServiceCollection services)
        {
            // Register sanitization service
            services.AddSingleton<ILogSanitizationService, LogSanitizationService>();
            return services;
        }

        /// <summary>
        /// Gets a sanitized logger for the specified type.
        /// Use this method when you need automatic log sanitization.
        /// </summary>
        public static ILogger<T> GetSanitizedLogger<T>(this IServiceProvider serviceProvider)
        {
            var innerLogger = serviceProvider.GetRequiredService<ILogger<T>>();
            var sanitizationService = serviceProvider.GetRequiredService<ILogSanitizationService>();
            return new SanitizedLogger<T>(innerLogger, sanitizationService);
        }
    }
}

