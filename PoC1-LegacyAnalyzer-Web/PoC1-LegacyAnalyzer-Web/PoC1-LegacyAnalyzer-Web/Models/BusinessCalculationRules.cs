namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents the business calculation rules configuration.
    /// </summary>
    public class BusinessCalculationRules
    {
        /// <summary>
        /// Cost calculation parameters.
        /// </summary>
        public CostCalculation CostCalculation { get; set; } = new CostCalculation();

        /// <summary>
        /// Complexity thresholds for project evaluation.
        /// </summary>
        public ComplexityThresholds ComplexityThresholds { get; set; } = new ComplexityThresholds();

        /// <summary>
        /// Risk thresholds for project evaluation.
        /// </summary>
        public RiskThresholds RiskThresholds { get; set; } = new RiskThresholds();

        /// <summary>
        /// Timeline estimation by complexity range.
        /// </summary>
        public Dictionary<string, string> TimelineEstimation { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Project size classification configuration.
        /// </summary>
        public Dictionary<string, ProjectSizeConfig> ProjectSizeClassification { get; set; } = new Dictionary<string, ProjectSizeConfig>();

        /// <summary>
        /// Agent weighting configuration.
        /// </summary>
        public AgentWeighting AgentWeighting { get; set; } = new AgentWeighting();

        /// <summary>
        /// Processing limits configuration.
        /// </summary>
        public ProcessingLimits ProcessingLimits { get; set; } = new ProcessingLimits();

        /// <summary>
        /// Architectural debt calculation configuration.
        /// </summary>
        public ArchitecturalDebtConfiguration ArchitecturalDebt { get; set; } = new ArchitecturalDebtConfiguration();

        /// <summary>
        /// Compliance cost avoidance configuration.
        /// </summary>
        public ComplianceCostConfiguration ComplianceCost { get; set; } = new ComplianceCostConfiguration();

        /// <summary>
        /// Manual analysis calculation configuration.
        /// </summary>
        public ManualAnalysisConfiguration ManualAnalysis { get; set; } = new ManualAnalysisConfiguration();

        /// <summary>
        /// Analysis limits and thresholds configuration.
        /// </summary>
        public AnalysisLimitsConfiguration AnalysisLimits { get; set; } = new AnalysisLimitsConfiguration();
    }
}