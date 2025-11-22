namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for various analysis limits and thresholds.
    /// </summary>
    public class AnalysisLimitsConfiguration
    {
        /// <summary>
        /// Folder complexity calculation configuration.
        /// </summary>
        public FolderComplexityConfig FolderComplexity { get; set; } = new FolderComplexityConfig();

        /// <summary>
        /// Architectural assessment configuration.
        /// </summary>
        public ArchitecturalAssessmentConfig ArchitecturalAssessment { get; set; } = new ArchitecturalAssessmentConfig();

        /// <summary>
        /// Investment priority fallback configuration.
        /// </summary>
        public InvestmentPriorityFallbackConfig InvestmentPriorityFallback { get; set; } = new InvestmentPriorityFallbackConfig();

        /// <summary>
        /// Analysis result limits.
        /// </summary>
        public AnalysisResultLimits ResultLimits { get; set; } = new AnalysisResultLimits();

        /// <summary>
        /// Business metrics calculation configuration.
        /// </summary>
        public BusinessMetricsCalculationConfig BusinessMetrics { get; set; } = new BusinessMetricsCalculationConfig();
    }

    /// <summary>
    /// Folder complexity calculation configuration.
    /// </summary>
    public class FolderComplexityConfig
    {
        /// <summary>
        /// Multiplier for file count in complexity calculation.
        /// </summary>
        public int FileCountMultiplier { get; set; } = 5;

        /// <summary>
        /// File count threshold for additional complexity.
        /// </summary>
        public int FileCountThreshold { get; set; } = 10;

        /// <summary>
        /// Additional complexity when threshold is exceeded.
        /// </summary>
        public int AdditionalComplexityWhenThresholdExceeded { get; set; } = 20;

        /// <summary>
        /// Maximum complexity score.
        /// </summary>
        public int MaxComplexityScore { get; set; } = 100;
    }

    /// <summary>
    /// Architectural assessment configuration.
    /// </summary>
    public class ArchitecturalAssessmentConfig
    {
        /// <summary>
        /// Excellent separation of concerns layer count threshold.
        /// </summary>
        public int ExcellentSeparationLayerCount { get; set; } = 4;

        /// <summary>
        /// Good separation of concerns layer count threshold.
        /// </summary>
        public int GoodSeparationLayerCount { get; set; } = 3;

        /// <summary>
        /// Basic separation of concerns layer count threshold.
        /// </summary>
        public int BasicSeparationLayerCount { get; set; } = 2;

        /// <summary>
        /// Test folder count threshold for good test coverage.
        /// </summary>
        public int GoodTestCoverageFolderCount { get; set; } = 2;
    }

    /// <summary>
    /// Investment priority fallback configuration.
    /// </summary>
    public class InvestmentPriorityFallbackConfig
    {
        /// <summary>
        /// High risk high value threshold.
        /// </summary>
        public decimal HighRiskHighValueThreshold { get; set; } = 100000m;

        /// <summary>
        /// Medium risk high value threshold.
        /// </summary>
        public decimal MediumRiskHighValueThreshold { get; set; } = 50000m;
    }

    /// <summary>
    /// Analysis result limits.
    /// </summary>
    public class AnalysisResultLimits
    {
        /// <summary>
        /// Maximum folders to analyze.
        /// </summary>
        public int MaxFoldersToAnalyze { get; set; } = 10;

        /// <summary>
        /// Maximum key classes per folder.
        /// </summary>
        public int MaxKeyClassesPerFolder { get; set; } = 5;

        /// <summary>
        /// Maximum next steps to generate.
        /// </summary>
        public int MaxNextSteps { get; set; } = 6;

        /// <summary>
        /// Maximum files for single agent analysis.
        /// </summary>
        public int MaxFilesForSingleAgentAnalysis { get; set; } = 10;

        /// <summary>
        /// Maximum files for multi-agent analysis.
        /// </summary>
        public int MaxFilesForMultiAgentAnalysis { get; set; } = 10;

        /// <summary>
        /// Default complexity score when simplified.
        /// </summary>
        public int DefaultComplexityScore { get; set; } = 50;
    }

    /// <summary>
    /// Business metrics calculation configuration.
    /// </summary>
    public class BusinessMetricsCalculationConfig
    {
        /// <summary>
        /// Base hours multiplier per method.
        /// </summary>
        public decimal BaseHoursPerMethod { get; set; } = 0.5m;

        /// <summary>
        /// Complexity multiplier base.
        /// </summary>
        public decimal ComplexityMultiplierBase { get; set; } = 0.5m;
    }
}

