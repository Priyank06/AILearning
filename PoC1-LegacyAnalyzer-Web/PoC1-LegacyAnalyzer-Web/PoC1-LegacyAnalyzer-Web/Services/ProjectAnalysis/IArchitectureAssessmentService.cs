using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;

namespace PoC1_LegacyAnalyzer_Web.Services.ProjectAnalysis
{
    public interface IArchitectureAssessmentService
    {
        Task<ProjectArchitectureAssessment> AssessProjectArchitectureAsync(
            ProjectAnalysisResult result,
            CancellationToken cancellationToken = default);
    }
}

