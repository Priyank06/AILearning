namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class TokenUsage
    {
        public string Provider { get; set; } = "semantic-kernel";
        public string Model { get; set; } = "unknown";
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
    }
}


