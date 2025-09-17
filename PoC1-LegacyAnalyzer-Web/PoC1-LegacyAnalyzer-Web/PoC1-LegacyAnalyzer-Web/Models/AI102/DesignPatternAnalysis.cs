namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DesignPatternAnalysis
    {
        public List<IdentifiedPattern> Patterns { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public double OverallQualityScore { get; set; }
        public Dictionary<string, int> PatternUsage { get; set; } = new();
    }
}