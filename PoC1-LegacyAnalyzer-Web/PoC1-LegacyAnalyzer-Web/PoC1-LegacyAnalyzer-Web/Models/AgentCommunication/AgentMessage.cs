namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class AgentMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string FromAgent { get; set; } = "";
        public string ToAgent { get; set; } = ""; // Empty for broadcast messages
        public MessageType Type { get; set; }
        public string Subject { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string ConversationId { get; set; } = "";
        public int Priority { get; set; } = 5; // 1-10 scale
    }

    public enum MessageType
    {
        Analysis,           // Sharing analysis results
        Question,           // Asking for clarification
        PeerReview,         // Reviewing another agent's work
        Recommendation,     // Providing recommendations
        Challenge,          // Challenging conclusions
        Agreement,          // Agreeing with analysis
        Synthesis,          // Combining multiple analyses
        FinalReport         // Final consolidated report
    }
}
