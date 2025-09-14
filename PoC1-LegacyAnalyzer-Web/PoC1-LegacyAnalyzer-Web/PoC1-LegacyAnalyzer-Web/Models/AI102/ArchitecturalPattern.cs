namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ArchitecturalPattern
    {
        public string PatternName { get; set; } = "";
        public string Description { get; set; } = "";
        public double Confidence { get; set; }
        public List<string> Components { get; set; } = new();
        public List<string> Relationships { get; set; } = new();
    }
}