namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Prompt templates for agent analysis and peer review.
    /// </summary>
    public class AgentPromptTemplate
    {
        /// <summary>
        /// Analysis prompt template.
        /// </summary>
        public string AnalysisPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Peer review prompt template.
        /// </summary>
        public string PeerReviewPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Default response message.
        /// </summary>
        public string DefaultResponse { get; set; } = string.Empty;
    }
}