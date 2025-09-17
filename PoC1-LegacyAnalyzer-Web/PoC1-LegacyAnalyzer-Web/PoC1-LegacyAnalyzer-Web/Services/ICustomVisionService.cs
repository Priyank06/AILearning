using PoC1_LegacyAnalyzer_Web.Models.AI102;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ICustomVisionService
    {
        Task<ClassificationResult> ClassifyArchitecturalPatternAsync(Stream imageStream);
        Task<List<CodePatternPrediction>> DetectCodePatternsAsync(Stream codeScreenshot);
        Task<DesignPatternAnalysis> AnalyzeDesignPatternsAsync(string sourceCode);
    }
}