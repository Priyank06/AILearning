namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class DiagramComponentsResult
    {
        public List<DiagramComponent> Components { get; set; } = new();
        public DiagramType DiagramType { get; set; }
        public double OverallConfidence { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}