namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for cost tracking and attribution.
    /// </summary>
    public class CostTrackingConfiguration
    {
        /// <summary>
        /// Enable or disable cost tracking.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Cost per 1K input tokens for GPT-4 (in USD).
        /// </summary>
        public decimal InputTokenCostPer1K { get; set; } = 0.03m; // GPT-4 pricing

        /// <summary>
        /// Cost per 1K output tokens for GPT-4 (in USD).
        /// </summary>
        public decimal OutputTokenCostPer1K { get; set; } = 0.06m; // GPT-4 pricing

        /// <summary>
        /// Cost per 1K input tokens for GPT-3.5 (in USD).
        /// </summary>
        public decimal InputTokenCostPer1K_GPT35 { get; set; } = 0.0015m;

        /// <summary>
        /// Cost per 1K output tokens for GPT-3.5 (in USD).
        /// </summary>
        public decimal OutputTokenCostPer1K_GPT35 { get; set; } = 0.002m;

        /// <summary>
        /// Default model to use for cost calculation.
        /// </summary>
        public string DefaultModel { get; set; } = "gpt-4";

        /// <summary>
        /// Whether to include cost in analysis results.
        /// </summary>
        public bool IncludeCostInResults { get; set; } = true;

        /// <summary>
        /// Whether to log cost metrics to Application Insights.
        /// </summary>
        public bool LogCostMetrics { get; set; } = true;
    }
}

