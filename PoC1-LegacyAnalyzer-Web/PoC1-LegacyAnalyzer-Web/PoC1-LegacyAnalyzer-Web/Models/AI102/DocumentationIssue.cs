namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DocumentationIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }
}