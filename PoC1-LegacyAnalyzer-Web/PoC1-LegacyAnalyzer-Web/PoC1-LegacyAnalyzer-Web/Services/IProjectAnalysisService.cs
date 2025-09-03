using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IProjectAnalysisService
    {
        Task<ProjectSummary> AnalyzeProjectFilesAsync(List<IBrowserFile> files, string analysisType, IProgress<int> progress = null);
    }

    public class ProjectSummary
    {
        public int TotalFiles { get; set; }
        public int TotalClasses { get; set; }
        public int TotalMethods { get; set; }
        public int TotalProperties { get; set; }
        public List<FileAnalysisResult> FileResults { get; set; } = new();
        public string OverallAssessment { get; set; } = "";
        public int ComplexityScore { get; set; }
        public string RiskLevel { get; set; } = "";
        public List<string> KeyRecommendations { get; set; } = new();
    }
}
