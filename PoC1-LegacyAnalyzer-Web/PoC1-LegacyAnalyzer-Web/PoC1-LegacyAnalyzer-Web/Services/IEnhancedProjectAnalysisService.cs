using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IEnhancedProjectAnalysisService
    {
        Task<ProjectAnalysisResult> AnalyzeProjectAsync(ProjectAnalysisRequest request, IProgress<ProjectAnalysisProgress> progress = null, CancellationToken cancellationToken = default);

        Task<ProjectMetadata> ExtractProjectMetadataAsync(List<IBrowserFile> files, CancellationToken cancellationToken = default);

        Task<string> GenerateProjectInsightsAsync(ProjectAnalysisResult analysis, string businessContext, CancellationToken cancellationToken = default);
    }

    public class ProjectAnalysisProgress
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public string CurrentFile { get; set; } = "";
        public string CurrentPhase { get; set; } = "";
        public string Status { get; set; } = "";
        public double ProgressPercentage => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
    }
}
