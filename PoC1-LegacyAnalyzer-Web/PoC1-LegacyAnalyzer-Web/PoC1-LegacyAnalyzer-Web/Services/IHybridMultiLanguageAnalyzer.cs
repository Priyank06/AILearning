using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Hybrid analyzer that combines Tree-sitter syntax analysis with AI-powered semantic analysis
    /// for non-C# languages (Python, JavaScript, TypeScript, Java, Go).
    /// </summary>
    public interface IHybridMultiLanguageAnalyzer
    {
        /// <summary>
        /// Performs hybrid analysis: Tree-sitter for syntax + AI for semantic issues.
        /// </summary>
        Task<SemanticAnalysisResult> AnalyzeAsync(
            string code,
            string fileName,
            LanguageKind language,
            string? analysisType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detects language-specific patterns (Python 2.x, var vs let/const, etc.)
        /// </summary>
        Task<List<LanguageSpecificPattern>> DetectLanguageSpecificPatternsAsync(
            string code,
            LanguageKind language,
            CancellationToken cancellationToken = default);
    }
}

