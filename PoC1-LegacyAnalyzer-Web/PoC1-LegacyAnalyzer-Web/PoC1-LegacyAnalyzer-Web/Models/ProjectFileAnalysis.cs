namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class ProjectFileAnalysis
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public CodeAnalysisResult Analysis { get; set; } = new();
        public string QuickInsight { get; set; } = string.Empty;
    }
}
