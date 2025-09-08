namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class AutonomousAnalysisResult
    {
        public string AgentReasoning { get; set; } = "";
        public string AnalysisStrategy { get; set; } = "";
        public List<string> ExecutionSteps { get; set; } = new();
        public string TechnicalFindings { get; set; } = "";
        public string BusinessImpact { get; set; } = "";
        public string StrategicRecommendations { get; set; } = "";
        public string ImplementationPlan { get; set; } = "";
        public int ConfidenceScore { get; set; }
        public string NextActions { get; set; } = "";
    }
}
