namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents cost metrics for an analysis operation.
    /// </summary>
    public class CostMetrics
    {
        /// <summary>
        /// Total input tokens used.
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Total output tokens used.
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Total tokens (input + output).
        /// </summary>
        public int TotalTokens => InputTokens + OutputTokens;

        /// <summary>
        /// Cost for input tokens (in USD).
        /// </summary>
        public decimal InputCost { get; set; }

        /// <summary>
        /// Cost for output tokens (in USD).
        /// </summary>
        public decimal OutputCost { get; set; }

        /// <summary>
        /// Total cost (in USD).
        /// </summary>
        public decimal TotalCost => InputCost + OutputCost;

        /// <summary>
        /// Model used for this analysis.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Analysis ID or conversation ID.
        /// </summary>
        public string AnalysisId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when cost was calculated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Breakdown by agent or operation.
        /// </summary>
        public Dictionary<string, CostMetrics> Breakdown { get; set; } = new();
    }
}

