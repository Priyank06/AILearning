namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DocumentationQualityResult
    {
        public double OverallScore { get; set; }
        public Dictionary<string, double> QualityMetrics { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<DocumentationIssue> IssuesFound { get; set; } = new();
    }
}