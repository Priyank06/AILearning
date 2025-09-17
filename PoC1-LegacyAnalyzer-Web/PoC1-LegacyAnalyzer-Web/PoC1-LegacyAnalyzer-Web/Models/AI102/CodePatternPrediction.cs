namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class CodePatternPrediction
    {
        public string PatternType { get; set; } = string.Empty;
        public BoundingBox Location { get; set; } = new();
        public double Confidence { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}