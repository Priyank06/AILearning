namespace PoC1_LegacyAnalyzer_Web.Services.Orchestration
{
    /// <summary>
    /// Service for rate limiting per-agent API calls to prevent overload.
    /// </summary>
    public interface IAgentRateLimiter
    {
        /// <summary>
        /// Waits if necessary to respect rate limits for the specified agent.
        /// </summary>
        Task WaitIfNeededAsync(string agentName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that an API call was made for the specified agent.
        /// </summary>
        void RecordApiCall(string agentName);

        /// <summary>
        /// Gets the current rate limit status for an agent.
        /// </summary>
        RateLimitStatus GetRateLimitStatus(string agentName);
    }

    /// <summary>
    /// Rate limit status for an agent.
    /// </summary>
    public class RateLimitStatus
    {
        public string AgentName { get; set; } = string.Empty;
        public int CallsInLastMinute { get; set; }
        public int MaxCallsPerMinute { get; set; } = 20; // Default: 20 calls per minute per agent
        public bool IsThrottled { get; set; }
        public TimeSpan? WaitTime { get; set; }
    }
}

