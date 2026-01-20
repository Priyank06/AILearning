using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.AI
{
    public interface IPromptBuilderService
    {
        string BuildAnalysisPrompt(string code, string analysisType, CodeAnalysisResult staticAnalysis);
        string BuildBatchAnalysisPrompt(List<(string fileName, string code, CodeAnalysisResult staticAnalysis)> fileAnalyses, string analysisType);
        string GetSystemPrompt(string analysisType);
        string GetBatchSystemPrompt(string analysisType);
    }
}

