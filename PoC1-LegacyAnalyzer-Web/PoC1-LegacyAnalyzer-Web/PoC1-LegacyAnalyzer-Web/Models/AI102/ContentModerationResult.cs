namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ContentModerationResult
    {
        public bool IsAppropriate { get; set; }
        public double ConfidenceScore { get; set; }
        public List<ModerationIssue> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}