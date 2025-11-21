namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Templates for orchestration prompts.
    /// </summary>
    public class OrchestrationPrompts
    {
        /// <summary>
        /// Template for creating an analysis plan.
        /// </summary>
        public string CreateAnalysisPlan { get; set; } = string.Empty;

        /// <summary>
        /// Template for facilitating agent discussion.
        /// </summary>
        public string FacilitateDiscussion { get; set; } = string.Empty;

        /// <summary>
        /// Template for synthesizing recommendations.
        /// </summary>
        public string SynthesizeRecommendations { get; set; } = string.Empty;

        /// <summary>
        /// Template for executive summary.
        /// </summary>
        public string CreateExecutiveSummary { get; set; } = string.Empty;

        /// <summary>
        /// Template for implementation strategy.
        /// </summary>
        public string CreateImplementationStrategy { get; set; } = string.Empty;
    }
}