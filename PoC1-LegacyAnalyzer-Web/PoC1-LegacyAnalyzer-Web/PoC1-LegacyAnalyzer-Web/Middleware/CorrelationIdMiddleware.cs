using PoC1_LegacyAnalyzer_Web.Services.Infrastructure;

namespace PoC1_LegacyAnalyzer_Web.Middleware
{
    /// <summary>
    /// Middleware to add correlation IDs to requests and responses.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITracingService tracingService)
        {
            // Get or create correlation ID
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            // Set in tracing service
            tracingService.SetCorrelationId(correlationId);

            // Add to response headers
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            // Add to Activity if available
            if (System.Diagnostics.Activity.Current != null)
            {
                System.Diagnostics.Activity.Current.SetTag("correlation.id", correlationId);
            }

            // Add to logger scope
            using (_logger.BeginScope(new Dictionary<string, object> { { "CorrelationId", correlationId } }))
            {
                await _next(context);
            }
        }
    }
}

