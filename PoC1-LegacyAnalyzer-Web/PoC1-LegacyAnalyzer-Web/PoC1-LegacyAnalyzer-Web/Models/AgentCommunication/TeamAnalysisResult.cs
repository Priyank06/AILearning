using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class PerformanceMetrics
    {
        public long PreprocessingTimeMs { get; set; }
        public long AgentAnalysisTimeMs { get; set; }
        public long PeerReviewTimeMs { get; set; }
        public long SynthesisTimeMs { get; set; }
        public long ExecutiveSummaryTimeMs { get; set; }
        public long TotalTimeMs { get; set; }
        public int TotalLLMCalls { get; set; }
        public long EstimatedSequentialTimeMs { get; set; }
        public double ParallelSpeedup { get; set; }
    }

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
