using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for managing and querying dependency graphs.
    /// Uses in-memory storage (can be extended to use persistent storage).
    /// </summary>
    public class DependencyGraphService : IDependencyGraphService
    {
        private readonly ICrossFileAnalyzer _crossFileAnalyzer;
        private readonly ILogger<DependencyGraphService> _logger;
        private readonly Dictionary<string, DependencyGraph> _graphs = new();
        private readonly Dictionary<string, Dictionary<string, DependencyImpact>> _impacts = new();

        public DependencyGraphService(
            ICrossFileAnalyzer crossFileAnalyzer,
            ILogger<DependencyGraphService> logger)
        {
            _crossFileAnalyzer = crossFileAnalyzer ?? throw new ArgumentNullException(nameof(crossFileAnalyzer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<DependencyGraph?> GetDependencyGraphAsync(string analysisId)
        {
            _graphs.TryGetValue(analysisId, out var graph);
            return Task.FromResult(graph);
        }

        public Task StoreDependencyGraphAsync(string analysisId, DependencyGraph graph)
        {
            _graphs[analysisId] = graph;
            _logger.LogInformation("Stored dependency graph for analysis {AnalysisId} with {NodeCount} nodes and {EdgeCount} edges",
                analysisId, graph.Nodes.Count, graph.Edges.Count);
            return Task.CompletedTask;
        }

        public async Task<DependencyImpact?> GetImpactAsync(string analysisId, string elementId)
        {
            if (!_impacts.ContainsKey(analysisId))
            {
                _impacts[analysisId] = new Dictionary<string, DependencyImpact>();
            }

            if (_impacts[analysisId].TryGetValue(elementId, out var cachedImpact))
            {
                return cachedImpact;
            }

            var graph = await GetDependencyGraphAsync(analysisId);
            if (graph == null)
            {
                return null;
            }

            var impact = await _crossFileAnalyzer.AnalyzeImpactAsync(graph, elementId);
            _impacts[analysisId][elementId] = impact;
            return impact;
        }

        public async Task<List<CyclicDependency>> GetCyclesAsync(string analysisId)
        {
            var graph = await GetDependencyGraphAsync(analysisId);
            if (graph == null)
            {
                return new List<CyclicDependency>();
            }

            return await _crossFileAnalyzer.DetectCyclesAsync(graph);
        }

        public async Task<List<DependencyNode>> GetGodObjectsAsync(string analysisId)
        {
            var graph = await GetDependencyGraphAsync(analysisId);
            if (graph == null)
            {
                return new List<DependencyNode>();
            }

            return await _crossFileAnalyzer.DetectGodObjectsAsync(graph);
        }
    }
}

