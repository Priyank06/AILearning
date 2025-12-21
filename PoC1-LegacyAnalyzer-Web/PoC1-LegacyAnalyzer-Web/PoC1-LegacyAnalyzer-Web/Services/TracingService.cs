using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for distributed tracing and correlation ID management.
    /// </summary>
    public interface ITracingService
    {
        /// <summary>
        /// Gets the current correlation ID.
        /// </summary>
        string? GetCorrelationId();

        /// <summary>
        /// Sets the correlation ID for the current context.
        /// </summary>
        void SetCorrelationId(string correlationId);

        /// <summary>
        /// Creates a new activity span for an operation.
        /// </summary>
        Activity? StartActivity(string operationName, string? parentId = null);

        /// <summary>
        /// Adds a tag to the current activity.
        /// </summary>
        void AddTag(string key, string? value);

        /// <summary>
        /// Adds an event to the current activity.
        /// </summary>
        void AddEvent(string eventName);
    }

    /// <summary>
    /// Implementation of tracing service using ActivitySource.
    /// </summary>
    public class TracingService : ITracingService
    {
        private static readonly ActivitySource ActivitySource = new("PoC1.LegacyAnalyzer.Web");
        private readonly TracingConfiguration _config;
        private readonly ILogger<TracingService> _logger;
        private static readonly AsyncLocal<string?> CorrelationIdContext = new();

        public TracingService(
            IOptions<TracingConfiguration> config,
            ILogger<TracingService> logger)
        {
            _config = config?.Value ?? new TracingConfiguration();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string? GetCorrelationId()
        {
            return CorrelationIdContext.Value ?? Activity.Current?.Id;
        }

        public void SetCorrelationId(string correlationId)
        {
            CorrelationIdContext.Value = correlationId;
        }

        public Activity? StartActivity(string operationName, string? parentId = null)
        {
            if (!_config.Enabled)
                return null;

            Activity? activity = null;

            if (!string.IsNullOrEmpty(parentId))
            {
                // Create activity with parent context
                var parentContext = new ActivityContext(
                    ActivityTraceId.CreateFromString(parentId.AsSpan(0, 32)),
                    ActivitySpanId.CreateFromString(parentId.AsSpan(32, 16)),
                    ActivityTraceFlags.Recorded);

                activity = ActivitySource.StartActivity(operationName, ActivityKind.Server, parentContext);
            }
            else
            {
                // Create new activity
                activity = ActivitySource.StartActivity(operationName, ActivityKind.Server);
            }

            if (activity != null)
            {
                // Add service tags
                activity.SetTag("service.name", _config.ServiceName);
                activity.SetTag("service.version", _config.ServiceVersion);
                activity.SetTag("operation.name", operationName);

                // Add correlation ID
                var correlationId = GetCorrelationId();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    activity.SetTag("correlation.id", correlationId);
                }
            }

            return activity;
        }

        public void AddTag(string key, string? value)
        {
            if (!_config.Enabled)
                return;

            Activity.Current?.SetTag(key, value);
        }

        public void AddEvent(string eventName)
        {
            if (!_config.Enabled)
                return;

            Activity.Current?.AddEvent(new ActivityEvent(eventName));
        }
    }
}

