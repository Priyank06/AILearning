using PoC1_LegacyAnalyzer_Web.Models.Caching;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services.Caching
{
    /// <summary>
    /// Service for caching agent analysis responses to reduce API costs and latency
    /// </summary>
    public interface IAgentResponseCacheService
    {
        /// <summary>
        /// Try to get a cached response for the given inputs
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="fileContent">Full file content</param>
        /// <param name="businessObjective">Business objective for analysis</param>
        /// <param name="model">LLM model used (e.g., "gpt-4")</param>
        /// <returns>Cached result if found and not expired, otherwise null</returns>
        Task<SpecialistAnalysisResult?> GetCachedResponseAsync(
            string agentName,
            string fileContent,
            string businessObjective,
            string model);

        /// <summary>
        /// Cache an agent response
        /// </summary>
        /// <param name="agentName">Name of the agent</param>
        /// <param name="specialty">Agent specialty</param>
        /// <param name="fileContent">Full file content</param>
        /// <param name="businessObjective">Business objective</param>
        /// <param name="model">LLM model used</param>
        /// <param name="analysisResult">The result to cache</param>
        /// <param name="tokenCount">Total tokens used</param>
        /// <param name="costInDollars">Cost of the analysis</param>
        /// <param name="fileName">File name (optional metadata)</param>
        /// <param name="language">Programming language (optional metadata)</param>
        Task CacheResponseAsync(
            string agentName,
            string specialty,
            string fileContent,
            string businessObjective,
            string model,
            SpecialistAnalysisResult analysisResult,
            int tokenCount,
            double costInDollars,
            string fileName = "",
            string language = "");

        /// <summary>
        /// Get cache statistics
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// Clear expired cache entries
        /// </summary>
        Task ClearExpiredEntriesAsync();

        /// <summary>
        /// Clear all cache entries
        /// </summary>
        Task ClearAllAsync();

        /// <summary>
        /// Get all cache entries (for debugging/monitoring)
        /// </summary>
        Task<List<AgentResponseCacheEntry>> GetAllEntriesAsync();
    }

    /// <summary>
    /// Statistics about cache performance
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Total number of cache entries
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Number of expired entries
        /// </summary>
        public int ExpiredEntries { get; set; }

        /// <summary>
        /// Total cache hits
        /// </summary>
        public int TotalHits { get; set; }

        /// <summary>
        /// Total cache misses
        /// </summary>
        public int TotalMisses { get; set; }

        /// <summary>
        /// Cache hit rate (0-100%)
        /// </summary>
        public double HitRate => TotalHits + TotalMisses > 0
            ? (TotalHits / (double)(TotalHits + TotalMisses)) * 100
            : 0;

        /// <summary>
        /// Total cost saved through caching (in dollars)
        /// </summary>
        public double TotalCostSavings { get; set; }

        /// <summary>
        /// Total tokens saved
        /// </summary>
        public int TotalTokensSaved { get; set; }

        /// <summary>
        /// Breakdown by agent
        /// </summary>
        public Dictionary<string, AgentCacheStatistics> StatsByAgent { get; set; } = new();
    }

    /// <summary>
    /// Cache statistics for a specific agent
    /// </summary>
    public class AgentCacheStatistics
    {
        public string AgentName { get; set; } = string.Empty;
        public int Entries { get; set; }
        public int Hits { get; set; }
        public double CostSavings { get; set; }
        public int TokensSaved { get; set; }
    }
}
