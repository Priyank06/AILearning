using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Business
{
    public interface IRecommendationGeneratorService
    {
        List<string> GenerateStrategicRecommendations(MultiFileAnalysisResult result, string analysisType);
        string GenerateExecutiveAssessment(MultiFileAnalysisResult result, string analysisType);
        string GenerateProjectSummary(MultiFileAnalysisResult result);
    }
}

