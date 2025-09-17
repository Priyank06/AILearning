namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class CodeInsightsResult
    {
        public List<CodeInsight> Insights { get; set; } = new();
        public Dictionary<string, int> PatternFrequency { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public double OverallQualityScore { get; set; }
    }
}