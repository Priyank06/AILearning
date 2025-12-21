namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents an error result from an agent with context and remediation information.
    /// </summary>
    public class AgentErrorResult
    {
        /// <summary>
        /// The specialty of the agent that failed.
        /// </summary>
        public string AgentSpecialty { get; set; } = string.Empty;

        /// <summary>
        /// Error code for support reference.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Detailed error description.
        /// </summary>
        public string ErrorDescription { get; set; } = string.Empty;

        /// <summary>
        /// Suggested remediation steps.
        /// </summary>
        public List<string> RemediationSteps { get; set; } = new();

        /// <summary>
        /// Whether the error is retryable.
        /// </summary>
        public bool IsRetryable { get; set; }

        /// <summary>
        /// Suggested retry delay in seconds.
        /// </summary>
        public int RetryDelaySeconds { get; set; }

        /// <summary>
        /// Exception details (sanitized).
        /// </summary>
        public string? ExceptionType { get; set; }

        /// <summary>
        /// Timestamp when error occurred.
        /// </summary>
        public DateTime ErrorTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Input summary that caused the error (sanitized, no code).
        /// </summary>
        public string? InputSummary { get; set; }
    }
}

