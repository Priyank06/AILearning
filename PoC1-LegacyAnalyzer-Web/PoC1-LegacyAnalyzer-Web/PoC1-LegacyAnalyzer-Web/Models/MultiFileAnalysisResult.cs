using PoC1_LegacyAnalyzer_Web.Services;

namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class MultiFileAnalysisResult
    {
        public int TotalFiles { get; set; }
        public int TotalClasses { get; set; }
        public int TotalMethods { get; set; }
        public int TotalProperties { get; set; }
        public int TotalUsingStatements { get; set; }
        public List<FileAnalysisResult> FileResults { get; set; } = new List<FileAnalysisResult>();
        public string OverallAssessment { get; set; } = string.Empty;
        public int OverallComplexityScore { get; set; }
        public string OverallRiskLevel { get; set; } = string.Empty;
        public List<string> KeyRecommendations { get; set; } = new List<string>();
        public string ProjectSummary { get; set; } = string.Empty;
    }
}
