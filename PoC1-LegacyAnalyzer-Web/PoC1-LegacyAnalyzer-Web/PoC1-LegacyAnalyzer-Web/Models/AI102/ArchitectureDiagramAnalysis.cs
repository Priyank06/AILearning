namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ArchitectureDiagramAnalysis
    {
        public List<DiagramComponent> Components { get; set; } = new();
        public List<DiagramConnection> Connections { get; set; } = new();
        public DiagramType DiagramType { get; set; }
        public double ConfidenceScore { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}