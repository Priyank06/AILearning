namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ConversationContext
    {
        public string SessionId { get; set; } = string.Empty;
        public List<ConversationTurn> History { get; set; } = new();
        public Dictionary<string, object> State { get; set; } = new();
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}