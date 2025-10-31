namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    /// <summary>
    /// Request object for AI agent analysis (optimized)
    /// </summary>
    public class AgentAnalysisRequest
    {
        // Compact project summary (~500 tokens)
        public ProjectSummary ProjectSummary { get; set; } = new();

        // Business context
        public string AnalysisObjective { get; set; } = string.Empty;
        public string BusinessContext { get; set; } = string.Empty;

        // Agent configuration
        public bool IncludeSecurityAnalysis { get; set; }
        public bool IncludePerformanceAnalysis { get; set; }
        public bool IncludeArchitectureAnalysis { get; set; }

        // Focus areas (to guide AI attention)
        public List<string> FocusAreas { get; set; } = new();
    }
}
