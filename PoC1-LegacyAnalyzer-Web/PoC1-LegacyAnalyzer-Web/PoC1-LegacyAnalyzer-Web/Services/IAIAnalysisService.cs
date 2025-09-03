using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface IAIAnalysisService
    {
        Task<string> GetAnalysisAsync(string code, string analysisType, CodeAnalysisResult staticAnalysis);
        string GetSystemPrompt(string analysisType);
    }
}
