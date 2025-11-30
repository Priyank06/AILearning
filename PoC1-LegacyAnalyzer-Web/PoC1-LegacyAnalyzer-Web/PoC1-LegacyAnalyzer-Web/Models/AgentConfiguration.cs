namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Root configuration for agent orchestration and analysis.
    /// </summary>
    public class AgentConfiguration
    {
        /// <summary>
        /// Profiles for each agent type (e.g., security, performance, architecture).
        /// </summary>
        public Dictionary<string, AgentProfile> AgentProfiles { get; set; } = new();

        /// <summary>
        /// Prompt templates for each agent type.
        /// </summary>
        public Dictionary<string, AgentPromptTemplate> AgentPromptTemplates { get; set; } = new();

        /// <summary>
        /// Orchestration prompt templates.
        /// </summary>
        public OrchestrationPrompts OrchestrationPrompts { get; set; } = new();

        /// <summary>
        /// Business impact rules for risk, priority, and effort estimation.
        /// </summary>
        public BusinessImpactRules BusinessImpactRules { get; set; } = new();

        /// <summary>
        /// Option to enable or disable orchestration synthesis for agent discussion. Default is false for PoC performance.
        /// </summary>
        public bool EnableDiscussionSynthesis { get; set; } = false;
    }

    public class PerformanceMetrics
    {
        public long PreprocessingTimeMs { get; set; }
        public long AgentAnalysisTimeMs { get; set; }
        public long PeerReviewTimeMs { get; set; }
        public long SynthesisTimeMs { get; set; }
        public long ExecutiveSummaryTimeMs { get; set; }
        public long TotalTimeMs { get; set; }
        public int TotalLLMCalls { get; set; }
        public long EstimatedSequentialTimeMs { get; set; }
        public double ParallelSpeedup { get; set; }
    }

    public class TeamAnalysisResult
    {
        // ...existing properties...
        public PerformanceMetrics PerformanceMetrics { get; set; } = new PerformanceMetrics();
    }
}