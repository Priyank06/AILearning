namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents risk thresholds.
    /// </summary>
    public class RiskThresholds
    {
        /// <summary>
        /// Maximum value for low risk.
        /// </summary>
        public int LowRiskMax { get; set; }

        /// <summary>
        /// Maximum value for medium risk.
        /// </summary>
        public int MediumRiskMax { get; set; }

        /// <summary>
        /// Minimum value for high risk.
        /// </summary>
        public int HighRiskMin { get; set; }
    }
}