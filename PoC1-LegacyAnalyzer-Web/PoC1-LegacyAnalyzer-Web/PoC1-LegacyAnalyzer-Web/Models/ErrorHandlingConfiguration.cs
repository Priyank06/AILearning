namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for error handling and recovery strategies.
    /// </summary>
    public class ErrorHandlingConfiguration
    {
        /// <summary>
        /// Enable or disable enhanced error handling.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Minimum number of successful agents required for partial success (e.g., 2 of 3).
        /// </summary>
        public int MinSuccessfulAgentsForPartialSuccess { get; set; } = 2;

        /// <summary>
        /// Whether to return partial results when some agents fail.
        /// </summary>
        public bool ReturnPartialResults { get; set; } = true;

        /// <summary>
        /// Whether to allow retry of individual failed agents.
        /// </summary>
        public bool AllowAgentRetry { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts per agent.
        /// </summary>
        public int MaxAgentRetryAttempts { get; set; } = 2;

        /// <summary>
        /// Whether to enable graceful degradation (continue with fewer agents if some fail).
        /// </summary>
        public bool EnableGracefulDegradation { get; set; } = true;

        /// <summary>
        /// Whether to include actionable remediation steps in error messages.
        /// </summary>
        public bool IncludeRemediationSteps { get; set; } = true;

        /// <summary>
        /// Whether to include error codes for support references.
        /// </summary>
        public bool IncludeErrorCodes { get; set; } = true;
    }
}

