namespace PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis
{
    public class ProjectAnalysisResult
    {
        public ProjectMetadata ProjectInfo { get; set; } = new();
        public Dictionary<string, FolderAnalysisResult> FolderAnalysis { get; set; } = new();
        public List<FileAnalysisResult> DetailedFileAnalysis { get; set; } = new();
        public ProjectArchitectureAssessment Architecture { get; set; } = new();
        public BusinessImpactAssessment BusinessImpact { get; set; } = new();
        public string ExecutiveSummary { get; set; } = "";
        public List<string> NextSteps { get; set; } = new();
    }
}
