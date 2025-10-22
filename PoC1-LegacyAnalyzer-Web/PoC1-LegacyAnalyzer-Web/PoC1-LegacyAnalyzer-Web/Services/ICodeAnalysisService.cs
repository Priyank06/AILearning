using Microsoft.CodeAnalysis;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service interface for code analysis operations.
    /// </summary>
    public interface ICodeAnalysisService
    {
        /// <summary>
        /// Analyzes C# source code and extracts structural information.
        /// </summary>
        /// <param name="code">C# source code to analyze</param>
        /// <returns>Analysis result containing classes, methods, and complexity metrics</returns>
        Task<CodeAnalysisResult> AnalyzeCodeAsync(string code);

        /// <summary>
        /// Performs basic structural analysis on a parsed syntax tree.
        /// </summary>
        /// <param name="root">Root node of the syntax tree</param>
        /// <returns>Analysis result with extracted code elements</returns>
        CodeAnalysisResult PerformBasicAnalysis(SyntaxNode root);
    }
}
