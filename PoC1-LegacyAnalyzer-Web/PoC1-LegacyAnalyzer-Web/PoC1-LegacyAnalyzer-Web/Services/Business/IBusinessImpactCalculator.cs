using PoC1_LegacyAnalyzer_Web.Models.ProjectAnalysis;

namespace PoC1_LegacyAnalyzer_Web.Services.Business
{
    public interface IBusinessImpactCalculator
    {
        Task<BusinessImpactAssessment> AssessBusinessImpactAsync(
            ProjectAnalysisResult result,
            CancellationToken cancellationToken = default);
    }
}

