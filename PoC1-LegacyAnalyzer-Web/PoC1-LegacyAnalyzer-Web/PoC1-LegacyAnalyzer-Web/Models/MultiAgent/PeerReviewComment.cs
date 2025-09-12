namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class PeerReviewComment
    {
        public string ReviewerAgent { get; set; } = "";
        public string Comment { get; set; } = "";
        public string Type { get; set; } = ""; // Agreement, Disagreement, Addition
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
