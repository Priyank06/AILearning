using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class ConsolidatedRecommendations
    {
        public List<Recommendation> HighPriorityActions { get; set; } = new();
        public List<Recommendation> MediumPriorityActions { get; set; } = new();
        public List<Recommendation> LongTermStrategic { get; set; } = new();
        public List<ConflictResolution> ResolvedConflicts { get; set; } = new();
        public decimal TotalEstimatedEffort { get; set; }
        public string ImplementationStrategy { get; set; } = "";
        public List<string> QuickWins { get; internal set; }
    }
}
