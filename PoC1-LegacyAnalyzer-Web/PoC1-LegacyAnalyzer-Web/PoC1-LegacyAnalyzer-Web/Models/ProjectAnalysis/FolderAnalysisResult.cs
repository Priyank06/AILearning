namespace PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis
{
    public class FolderAnalysisResult
    {
        public string FolderName { get; set; } = "";
        public int FileCount { get; set; }
        public int TotalLines { get; set; }
        public string Purpose { get; set; } = "";
        public List<string> KeyClasses { get; set; } = new();
        public string ArchitecturalRole { get; set; } = "";
        public int ComplexityScore { get; set; }
    }
}
