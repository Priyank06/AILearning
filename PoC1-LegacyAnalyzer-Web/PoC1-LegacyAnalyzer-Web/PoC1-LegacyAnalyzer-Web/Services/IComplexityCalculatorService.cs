using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IComplexityCalculatorService
    {
        int CalculateFileComplexity(CodeAnalysisResult analysis);
        int CalculateProjectComplexity(MultiFileAnalysisResult result);
    }
}

