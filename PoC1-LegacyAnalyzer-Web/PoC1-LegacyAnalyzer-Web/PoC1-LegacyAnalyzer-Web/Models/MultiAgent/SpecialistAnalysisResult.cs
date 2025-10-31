namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class SpecialistAnalysisResult
    {
        public string AgentName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public DateTime AnalysisTimestamp { get; set; } = DateTime.Now;
        public int ConfidenceScore { get; set; }

        // Core Analysis Results
        public List<Finding> KeyFindings { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public RiskAssessment RiskLevel { get; set; } = new();

        // Agent-Specific Metrics
        public Dictionary<string, object> SpecialtyMetrics { get; set; } = new();

        // Peer Review Capabilities
        public List<PeerReviewComment> PeerReviews { get; set; } = new();

        // Business Context
        public string BusinessImpact { get; set; } = "";
        public decimal EstimatedEffort { get; set; }
        public string Priority { get; set; } = "";
    }
}
