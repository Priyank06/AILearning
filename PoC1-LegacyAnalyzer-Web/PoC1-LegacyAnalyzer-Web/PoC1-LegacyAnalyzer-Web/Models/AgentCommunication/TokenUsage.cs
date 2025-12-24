namespace PoC1_LegacyAnalyzer_Web.Models.AgentCommunication
{
    public class TokenUsage
    {
        public string Provider { get; set; } = string.Empty; // Default should be set from configuration
        public string Model { get; set; } = string.Empty; // Default should be set from configuration
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens => PromptTokens + CompletionTokens;
    }
}


