namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class ConflictResolution
    {
        public string ConflictDescription { get; set; } = "";
        public List<string> ConflictingAgents { get; set; } = new();
        public string Resolution { get; set; } = "";
        public string Rationale { get; set; } = "";
        public int ConfidenceInResolution { get; set; }
    }
}
