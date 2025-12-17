using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class TeamAnalysisResult
    {
        public string ConversationId { get; set; } = "";
        public List<SpecialistAnalysisResult> IndividualAnalyses { get; set; } = new();
        public List<AgentMessage> TeamDiscussion { get; set; } = new();
        public ConsolidatedRecommendations FinalRecommendations { get; set; } = new();
        public TeamConsensusMetrics Consensus { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public int OverallConfidenceScore { get; set; }
        public string ExecutiveSummary { get; set; } = "";
        public TokenUsage? TokenUsage { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; } = new PerformanceMetrics();        
    }
}
