using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using PoC1_LegacyAnalyzer_Web.Services.Infrastructure;
using System.Net;

namespace PoC1_LegacyAnalyzer_Web.Middleware
{
    /// <summary>
    /// Middleware to enforce rate limiting on HTTP requests.
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly RateLimitConfiguration _config;
        private readonly IRateLimitService _rateLimitService;

        public RateLimitMiddleware(
            RequestDelegate next,
            ILogger<RateLimitMiddleware> logger,
            IOptions<RateLimitConfiguration> config,
            IRateLimitService rateLimitService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip rate limiting if disabled
            if (!_config.Enabled)
            {
                await _next(context);
                return;
            }

            // Skip rate limiting for excluded paths
            var path = context.Request.Path.Value ?? string.Empty;
            if (_config.ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Get client identifier (IP address for now, can be enhanced with user ID when auth is added)
            var clientId = GetClientId(context);

            // Check if request is allowed
            if (!_rateLimitService.IsRequestAllowed(clientId))
            {
                var remaining = _rateLimitService.GetRemainingRequests(clientId);
                var resetTime = _rateLimitService.GetResetTimeSeconds(clientId);

                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId} on path {Path}. Remaining: {Remaining}, Reset in: {ResetTime}s",
                    clientId,
                    path,
                    remaining,
                    resetTime);

                context.Response.StatusCode = _config.RateLimitExceededStatusCode;
                context.Response.ContentType = "application/json";

                // Add rate limit headers
                context.Response.Headers.Add("X-RateLimit-Limit", _config.MaxRequestsPerWindow.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", resetTime.ToString());
                context.Response.Headers.Add("Retry-After", resetTime.ToString());

                var response = new
                {
                    error = _config.RateLimitExceededMessage,
                    retryAfter = resetTime,
                    rateLimit = new
                    {
                        limit = _config.MaxRequestsPerWindow,
                        remaining = remaining,
                        resetInSeconds = resetTime
                    }
                };

                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            // Request allowed, add rate limit headers and continue
            var remainingRequests = _rateLimitService.GetRemainingRequests(clientId);
            var resetTimeSeconds = _rateLimitService.GetResetTimeSeconds(clientId);

            context.Response.Headers.Add("X-RateLimit-Limit", _config.MaxRequestsPerWindow.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", remainingRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", resetTimeSeconds.ToString());

            await _next(context);
        }

        /// <summary>
        /// Gets a unique identifier for the client making the request.
        /// </summary>
        private string GetClientId(HttpContext context)
        {
            // Try to get user ID if authenticated (for future use)
            // var userId = context.User?.Identity?.Name;
            // if (!string.IsNullOrEmpty(userId))
            //     return userId;

            // Fall back to IP address
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Handle forwarded headers (for reverse proxy scenarios)
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Take the first IP in the chain
                    ipAddress = forwardedFor.Split(',')[0].Trim();
                }
            }

            return ipAddress;
        }
    }
}

