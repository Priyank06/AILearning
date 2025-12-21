namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for distributed tracing.
    /// </summary>
    public class TracingConfiguration
    {
        /// <summary>
        /// Enable or disable distributed tracing.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Service name for tracing.
        /// </summary>
        public string ServiceName { get; set; } = "LegacyAnalyzer-Web";

        /// <summary>
        /// Service version for tracing.
        /// </summary>
        public string ServiceVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Whether to include correlation IDs in logs.
        /// </summary>
        public bool IncludeCorrelationIdInLogs { get; set; } = true;

        /// <summary>
        /// Whether to create custom spans for agent operations.
        /// </summary>
        public bool CreateAgentSpans { get; set; } = true;

        /// <summary>
        /// Whether to create spans for preprocessing operations.
        /// </summary>
        public bool CreatePreprocessingSpans { get; set; } = true;
    }
}

