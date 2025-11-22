namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for manual analysis calculations.
    /// </summary>
    public class ManualAnalysisConfiguration
    {
        /// <summary>
        /// Hours per class for manual analysis.
        /// </summary>
        public decimal HoursPerClass { get; set; } = 2m;

        /// <summary>
        /// Hours per method for manual analysis.
        /// </summary>
        public decimal HoursPerMethod { get; set; } = 0.25m;

        /// <summary>
        /// Hours per dependency for manual analysis.
        /// </summary>
        public decimal HoursPerDependency { get; set; } = 0.5m;

        /// <summary>
        /// AI analysis time in minutes.
        /// </summary>
        public int AIAnalysisTimeMinutes { get; set; } = 3;

        /// <summary>
        /// AI analysis cost per assessment.
        /// </summary>
        public decimal AIAnalysisCostPerAssessment { get; set; } = 5m;
    }
}

