namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents the templates for analysis prompts.
    /// </summary>
    public class AnalysisPromptTemplates
    {
        /// <summary>
        /// The base template string with placeholders.
        /// </summary>
        public string BaseTemplate { get; set; } = string.Empty;

        /// <summary>
        /// Dictionary of analysis type to its prompt sections.
        /// </summary>
        public Dictionary<string, AnalysisPromptSections> Templates { get; set; } = new();
    }
}