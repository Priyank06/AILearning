namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class EntityExtraction
    {
        public string EntityType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Context { get; set; } = string.Empty;
    }
}