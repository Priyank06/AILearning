using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Service for analyzing cross-file dependencies in codebases.
    /// Uses Roslyn semantic model for C# and Tree-sitter for other languages (Python, JavaScript, TypeScript, Java, Go).
    /// Detects method calls, inheritance, imports, and other dependencies.
    /// </summary>
    public interface ICrossFileAnalyzer
    {
        /// <summary>
        /// Builds a dependency graph from a collection of C# files.
        /// </summary>
        Task<DependencyGraph> BuildDependencyGraphAsync(List<IBrowserFile> files, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes impact of changing a specific code element.
        /// </summary>
        Task<DependencyImpact> AnalyzeImpactAsync(DependencyGraph graph, string elementId);

        /// <summary>
        /// Detects cyclic dependencies in the graph.
        /// </summary>
        Task<List<CyclicDependency>> DetectCyclesAsync(DependencyGraph graph);

        /// <summary>
        /// Identifies God Objects (classes with high connectivity).
        /// </summary>
        Task<List<DependencyNode>> DetectGodObjectsAsync(DependencyGraph graph, int threshold = 20);
    }
}

