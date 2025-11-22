namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for agent-specific analysis parameters.
    /// </summary>
    public class AgentAnalysisConfiguration
    {
        /// <summary>
        /// Security analyst configuration.
        /// </summary>
        public SecurityAnalystConfig Security { get; set; } = new SecurityAnalystConfig();

        /// <summary>
        /// Performance analyst configuration.
        /// </summary>
        public PerformanceAnalystConfig Performance { get; set; } = new PerformanceAnalystConfig();

        /// <summary>
        /// Architectural analyst configuration.
        /// </summary>
        public ArchitecturalAnalystConfig Architecture { get; set; } = new ArchitecturalAnalystConfig();
    }

    /// <summary>
    /// Security analyst specific configuration.
    /// </summary>
    public class SecurityAnalystConfig
    {
        /// <summary>
        /// Confidence score calculation multiplier.
        /// </summary>
        public int ConfidenceScoreMultiplier { get; set; } = 20;

        /// <summary>
        /// Minimum analysis length for confidence scoring.
        /// </summary>
        public int MinAnalysisLengthForConfidence { get; set; } = 500;

        /// <summary>
        /// Effort estimation values by complexity count.
        /// </summary>
        public Dictionary<int, decimal> EffortEstimationByComplexity { get; set; } = new Dictionary<int, decimal>
        {
            { 0, 8m },
            { 1, 16m },
            { 2, 40m },
            { 3, 80m }
        };

        /// <summary>
        /// Risk level thresholds by risk factor count.
        /// </summary>
        public Dictionary<int, string> RiskLevelByFactorCount { get; set; } = new Dictionary<int, string>
        {
            { 0, "LOW" },
            { 1, "MEDIUM" },
            { 2, "HIGH" },
            { 3, "CRITICAL" }
        };

        /// <summary>
        /// Maximum evidence items to extract.
        /// </summary>
        public int MaxEvidenceItems { get; set; } = 3;
    }

    /// <summary>
    /// Performance analyst specific configuration.
    /// </summary>
    public class PerformanceAnalystConfig
    {
        /// <summary>
        /// Confidence score calculation multiplier.
        /// </summary>
        public int ConfidenceScoreMultiplier { get; set; } = 16;

        /// <summary>
        /// Minimum analysis length for confidence scoring.
        /// </summary>
        public int MinAnalysisLengthForConfidence { get; set; } = 600;

        /// <summary>
        /// Effort estimation values by complexity count.
        /// </summary>
        public Dictionary<int, decimal> EffortEstimationByComplexity { get; set; } = new Dictionary<int, decimal>
        {
            { 0, 4m },
            { 1, 12m },
            { 2, 24m },
            { 3, 40m },
            { 4, 80m }
        };
    }

    /// <summary>
    /// Architectural analyst specific configuration.
    /// </summary>
    public class ArchitecturalAnalystConfig
    {
        /// <summary>
        /// Confidence score calculation multiplier.
        /// </summary>
        public int ConfidenceScoreMultiplier { get; set; } = 14;

        /// <summary>
        /// Minimum analysis length for confidence scoring.
        /// </summary>
        public int MinAnalysisLengthForConfidence { get; set; } = 800;

        /// <summary>
        /// Effort estimation values by complexity count.
        /// </summary>
        public Dictionary<int, decimal> EffortEstimationByComplexity { get; set; } = new Dictionary<int, decimal>
        {
            { 1, 16m },
            { 2, 40m },
            { 3, 80m },
            { 4, 160m },
            { 5, 320m }
        };
    }
}

