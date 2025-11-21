namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Business impact rules for risk mapping, priority, and effort estimation.
    /// </summary>
    public class BusinessImpactRules
    {
        /// <summary>
        /// List of risk pattern mappings.
        /// </summary>
        public List<RiskMapping> RiskLevelMapping { get; set; } = new();

        /// <summary>
        /// List of investment priority rules.
        /// </summary>
        public List<PriorityRule> InvestmentPriorityRules { get; set; } = new();

        /// <summary>
        /// Dictionary of effort estimation factors.
        /// </summary>
        public Dictionary<string, double> EffortEstimationFactors { get; set; } = new();
    }
}