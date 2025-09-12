namespace PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis
{
    public class ProjectArchitectureAssessment
    {
        public string ArchitecturalPattern { get; set; } = "";
        public List<string> LayerIdentification { get; set; } = new();
        public string SeparationOfConcerns { get; set; } = "";
        public List<string> DesignPatterns { get; set; } = new();
        public string TestCoverage { get; set; } = "";
        public int ArchitecturalDebtScore { get; set; }
    }
}
