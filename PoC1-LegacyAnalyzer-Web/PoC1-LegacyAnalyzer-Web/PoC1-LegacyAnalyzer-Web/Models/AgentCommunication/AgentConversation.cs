namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class AgentConversation
    {
        public string ConversationId { get; set; } = Guid.NewGuid().ToString();
        public string Topic { get; set; } = "";
        public List<string> ParticipantAgents { get; set; } = new();
        public List<AgentMessage> Messages { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public ConversationStatus Status { get; set; } = ConversationStatus.Active;
        public string Summary { get; set; } = "";
        public Dictionary<string, object> ConversationMetadata { get; set; } = new();
    }

    public enum ConversationStatus
    {
        Active,
        Completed,
        Paused,
        Failed
    }
}
