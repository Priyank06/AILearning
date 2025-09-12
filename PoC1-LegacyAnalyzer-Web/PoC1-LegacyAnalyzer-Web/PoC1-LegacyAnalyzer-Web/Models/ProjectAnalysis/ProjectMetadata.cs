namespace PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis
{
    public class ProjectMetadata
    {
        public string SolutionName { get; set; } = "";
        public List<string> ProjectFiles { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string TargetFramework { get; set; } = "";
        public int TotalLines { get; set; }
        public DateTime LastModified { get; set; }
        public List<string> MainNamespaces { get; set; } = new();
    }
}
