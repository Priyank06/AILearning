namespace PoC1_LegacyAnalyzer_Web.Models.AI102
{
    public class ArchitectureDocumentInsights
    {
        public string DocumentType { get; set; } = "";
        public List<string> IdentifiedComponents { get; set; } = new();
        public List<string> TechnicalRequirements { get; set; } = new();
        public List<string> BusinessRules { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }
}