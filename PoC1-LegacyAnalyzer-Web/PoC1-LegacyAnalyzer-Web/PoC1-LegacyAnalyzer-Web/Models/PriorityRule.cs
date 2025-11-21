namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Defines a condition-action pair for investment priority.
    /// </summary>
    public class PriorityRule
    {
        /// <summary>
        /// Condition expression (e.g., "riskLevel == 'Critical'").
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Action to take (e.g., "Immediate remediation required").
        /// </summary>
        public string Action { get; set; } = string.Empty;
    }
}