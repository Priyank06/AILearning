namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for rate limiting enforcement.
    /// </summary>
    public class RateLimitConfiguration
    {
        /// <summary>
        /// Enable or disable rate limiting.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum requests per minute per client.
        /// </summary>
        public int MaxRequestsPerMinute { get; set; } = 10;

        /// <summary>
        /// Window size in seconds for sliding window algorithm.
        /// </summary>
        public int WindowSizeSeconds { get; set; } = 60;

        /// <summary>
        /// Maximum number of requests allowed in the window.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 10;

        /// <summary>
        /// Whether to queue requests when rate limit is exceeded (true) or reject immediately (false).
        /// </summary>
        public bool QueueRequestsWhenExceeded { get; set; } = false;

        /// <summary>
        /// Maximum queue wait time in seconds when queueing is enabled.
        /// </summary>
        public int MaxQueueWaitSeconds { get; set; } = 30;

        /// <summary>
        /// Paths to exclude from rate limiting (e.g., health checks, static files).
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new() { "/health", "/_blazor", "/css", "/js", "/favicon.ico" };

        /// <summary>
        /// HTTP status code to return when rate limit is exceeded.
        /// </summary>
        public int RateLimitExceededStatusCode { get; set; } = 429;

        /// <summary>
        /// Message to include in rate limit response.
        /// </summary>
        public string RateLimitExceededMessage { get; set; } = "Rate limit exceeded. Please try again later.";
    }
}

