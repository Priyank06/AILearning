using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IProjectInsightsGenerator
    {
        Task<string> GenerateProjectInsightsAsync(
            ProjectAnalysisResult analysis,
            string businessContext,
            CancellationToken cancellationToken = default);
    }
}

