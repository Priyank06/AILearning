namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class IdentifiedPattern
    {
        public string PatternName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public PatternQuality Quality { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }
}