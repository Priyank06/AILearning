using Microsoft.CodeAnalysis;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public interface ICodeAnalysisService
    {
        Task<CodeAnalysisResult> AnalyzeCodeAsync(string code);
        CodeAnalysisResult PerformBasicAnalysis(SyntaxNode root);
    }
}
