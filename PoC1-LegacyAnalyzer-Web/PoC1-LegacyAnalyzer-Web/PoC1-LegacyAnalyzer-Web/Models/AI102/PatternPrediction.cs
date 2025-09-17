namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class PatternPrediction
    {
        public string PatternName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
    }
}