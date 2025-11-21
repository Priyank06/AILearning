namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Maps a risk pattern to a risk level.
    /// </summary>
    public class RiskMapping
    {
        /// <summary>
        /// Pattern to match (e.g., "SQL Injection").
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Associated risk level (e.g., "High").
        /// </summary>
        public string RiskLevel { get; set; } = string.Empty;
    }
}