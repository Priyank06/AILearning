namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class CodePatternAnalysis
    {
        public List<string> SecurityFindings { get; set; } = new();
        public List<string> PerformanceFindings { get; set; } = new();
        public List<string> ArchitectureFindings { get; set; } = new();
    }
}

