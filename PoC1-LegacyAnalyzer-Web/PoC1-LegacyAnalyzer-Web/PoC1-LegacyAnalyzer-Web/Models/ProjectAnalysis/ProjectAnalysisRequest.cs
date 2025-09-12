using Microsoft.AspNetCore.Components.Forms;

namespace PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis
{
    public class ProjectAnalysisRequest
    {
        public List<IBrowserFile> Files { get; set; } = new();
        public string ProjectPath { get; set; } = "";
        public string ProjectType { get; set; } = "";
        public string AnalysisType { get; set; } = "";
        public string AnalysisMode { get; set; } = "";
        public Dictionary<string, List<IBrowserFile>> ProjectStructure { get; set; } = new();
        public ProjectMetadata Metadata { get; set; } = new();
    }
}
