using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    public interface IComplexityCalculatorService
    {
        int CalculateFileComplexity(CodeAnalysisResult analysis);
        int CalculateProjectComplexity(MultiFileAnalysisResult result);
    }
}

