using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Service for managing and querying dependency graphs.
    /// </summary>
    public interface IDependencyGraphService
    {
        /// <summary>
        /// Gets the dependency graph for a set of files.
        /// </summary>
        Task<DependencyGraph?> GetDependencyGraphAsync(string analysisId);

        /// <summary>
        /// Stores a dependency graph for later retrieval.
        /// </summary>
        Task StoreDependencyGraphAsync(string analysisId, DependencyGraph graph);

        /// <summary>
        /// Gets impact analysis for a specific element.
        /// </summary>
        Task<DependencyImpact?> GetImpactAsync(string analysisId, string elementId);

        /// <summary>
        /// Gets all cyclic dependencies.
        /// </summary>
        Task<List<CyclicDependency>> GetCyclesAsync(string analysisId);

        /// <summary>
        /// Gets all God Objects.
        /// </summary>
        Task<List<DependencyNode>> GetGodObjectsAsync(string analysisId);
    }
}

