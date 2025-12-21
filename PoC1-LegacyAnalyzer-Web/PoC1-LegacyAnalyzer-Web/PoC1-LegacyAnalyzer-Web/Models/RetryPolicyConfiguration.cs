namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for retry policies and circuit breakers for Azure OpenAI API calls.
    /// </summary>
    public class RetryPolicyConfiguration
    {
        /// <summary>
        /// Maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Initial delay in seconds before first retry.
        /// </summary>
        public int InitialDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Maximum delay in seconds between retries (exponential backoff cap).
        /// </summary>
        public int MaxDelaySeconds { get; set; } = 30;

        /// <summary>
        /// Circuit breaker: Number of consecutive failures before opening circuit.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Circuit breaker: Duration in seconds to keep circuit open before attempting to close.
        /// </summary>
        public int CircuitBreakerDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Circuit breaker: Number of successful calls needed to close circuit after half-open state.
        /// </summary>
        public int CircuitBreakerSuccessThreshold { get; set; } = 2;

        /// <summary>
        /// HTTP status codes that should trigger retry (e.g., 429, 500, 502, 503, 504).
        /// </summary>
        public List<int> RetryableStatusCodes { get; set; } = new() { 429, 500, 502, 503, 504 };

        /// <summary>
        /// HTTP status codes that should NOT trigger retry (e.g., 400, 401, 403, 404).
        /// </summary>
        public List<int> NonRetryableStatusCodes { get; set; } = new() { 400, 401, 403, 404 };
    }
}

