namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    /// <summary>
    /// Aggregated summary of entire project for AI analysis
    /// </summary>
    public class ProjectSummary
    {
        public int TotalFiles { get; set; }
        public int TotalLines { get; set; }
        public int TotalClasses { get; set; }
        public int TotalMethods { get; set; }
        public double AverageComplexityScore { get; set; }

        public List<string> TopComplexFiles { get; set; } = new();
        public List<string> CommonDependencies { get; set; } = new();
        public List<CodeIssue> PreIdentifiedIssues { get; set; } = new();

        public List<FileMetadata> FileSummaries { get; set; } = new();

        public string OverallComplexity { get; set; } = "Medium";
        
        public string StructuredSummary { get; set; } = string.Empty;        
    }
}
