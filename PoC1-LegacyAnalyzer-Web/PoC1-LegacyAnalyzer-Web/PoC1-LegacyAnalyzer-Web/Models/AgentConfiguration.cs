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
    }
}