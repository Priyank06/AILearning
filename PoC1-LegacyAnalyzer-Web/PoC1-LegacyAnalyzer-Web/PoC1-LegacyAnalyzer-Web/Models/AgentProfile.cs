namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Describes an agent's profile and specialty.
    /// </summary>
    public class AgentProfile
    {
        /// <summary>
        /// Unique agent name.
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Agent's specialty or role.
        /// </summary>
        public string Specialty { get; set; } = string.Empty;

        /// <summary>
        /// Detailed persona description.
        /// </summary>
        public string AgentPersona { get; set; } = string.Empty;

        /// <summary>
        /// Confidence threshold for agent actions.
        /// </summary>
        public int ConfidenceThreshold { get; set; }
    }
}