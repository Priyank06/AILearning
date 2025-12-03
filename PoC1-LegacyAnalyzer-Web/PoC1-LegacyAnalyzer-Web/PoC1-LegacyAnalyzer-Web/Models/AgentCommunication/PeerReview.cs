namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class PeerReview
    {
        public string Reviewer { get; set; } = "";
        public string Reviewee { get; set; } = "";
        public string Comments { get; set; } = "";
        public bool IsApproved { get; set; }
        public DateTime ReviewTimestamp { get; set; } = DateTime.Now;
    }
}

