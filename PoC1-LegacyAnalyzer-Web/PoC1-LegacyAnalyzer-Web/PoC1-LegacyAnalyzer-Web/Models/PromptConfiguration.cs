namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Represents the configuration for prompt settings loaded from JSON.
    /// </summary>
    public class PromptConfiguration
    {
        /// <summary>
        /// Maximum length for code preview.
        /// </summary>
        public int CodePreviewMaxLength { get; set; } = 1200;

        /// <summary>
        /// Maximum tokens allowed for quick insight.
        /// </summary>
        public int MaxTokensForQuickInsight { get; set; } = 60;

        /// <summary>
        /// Temperature value for quick insight analysis.
        /// </summary>
        public float TemperatureForQuickInsight { get; set; } = 0.2f;

        /// <summary>
        /// System prompt messages for different analysis types.
        /// </summary>
        public Dictionary<string, string> SystemPrompts { get; set; } = new();

        /// <summary>
        /// Templates for analysis prompts.
        /// </summary>
        public AnalysisPromptTemplates AnalysisPromptTemplates { get; set; } = new();

        /// <summary>
        /// Error messages for various failure scenarios.
        /// </summary>
        public Dictionary<string, string> ErrorMessages { get; set; } = new();
    }
}