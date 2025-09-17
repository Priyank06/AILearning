namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ConversationTurn
    {
        public string UserInput { get; set; } = string.Empty;
        public AnalysisIntent Intent { get; set; } = new();
        public string BotResponse { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}