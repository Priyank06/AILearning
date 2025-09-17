namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ModerationIssue
    {
        public IssueType Type { get; set; }
        public IssueSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}