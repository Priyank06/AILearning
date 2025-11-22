namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for compliance cost avoidance calculations.
    /// </summary>
    public class ComplianceCostConfiguration
    {
        /// <summary>
        /// Compliance cost avoidance by risk level.
        /// </summary>
        public Dictionary<string, decimal> CostAvoidanceByRiskLevel { get; set; } = new Dictionary<string, decimal>
        {
            { "HIGH", 15000m },
            { "MEDIUM", 8000m },
            { "LOW", 3000m },
            { "DEFAULT", 1000m }
        };
    }
}

