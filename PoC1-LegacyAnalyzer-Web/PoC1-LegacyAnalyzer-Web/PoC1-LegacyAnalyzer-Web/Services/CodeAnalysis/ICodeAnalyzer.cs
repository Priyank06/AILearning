using System.Threading;
using System.Threading.Tasks;
using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Unified interface for language-specific analyzers (Roslyn-based for C#, Tree-sitter for others).
    /// </summary>
    public interface ICodeAnalyzer
    {
        /// <summary>
        /// Analyzes a single file and produces a unified CodeStructure and CodeAnalysisResult.
        /// </summary>
        Task<(CodeStructure structure, CodeAnalysisResult summary)> AnalyzeAsync(
            AnalyzableFile file,
            CancellationToken cancellationToken = default);
    }
}


