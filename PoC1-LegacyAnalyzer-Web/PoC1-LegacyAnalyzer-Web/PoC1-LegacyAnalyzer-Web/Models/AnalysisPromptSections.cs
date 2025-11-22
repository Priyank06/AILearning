namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents the sections for a specific analysis prompt.
    /// </summary>
    public class AnalysisPromptSections
    {
        /// <summary>
        /// List of assessment items for the analysis type.
        /// </summary>
        public List<string> Sections { get; set; } = new();
    }
}