using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Models.Caching
{
    /// <summary>
    /// Represents a cached agent analysis response
    /// </summary>
    public class AgentResponseCacheEntry
    {
        /// <summary>
        /// Cache key (hash of agentName + fileContent + businessObjective)
        /// </summary>
        public string CacheKey { get; set; } = string.Empty;

        /// <summary>
        /// Agent that produced this response
        /// </summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>
        /// Agent specialty
        /// </summary>
        public string Specialty { get; set; } = string.Empty;

        /// <summary>
        /// The cached analysis result
        /// </summary>
        public SpecialistAnalysisResult AnalysisResult { get; set; } = new();

        /// <summary>
        /// When this was cached
        /// </summary>
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this cache entry expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// File content hash (SHA256)
        /// </summary>
        public string FileContentHash { get; set; } = string.Empty;

        /// <summary>
        /// Business objective hash (for matching)
        /// </summary>
        public string BusinessObjectiveHash { get; set; } = string.Empty;

        /// <summary>
        /// Model used for this analysis (e.g., "gpt-4", "gpt-3.5-turbo")
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Token count (input + output)
        /// </summary>
        public int TokenCount { get; set; }

        /// <summary>
        /// Cost of this analysis (in dollars)
        /// </summary>
        public double CostInDollars { get; set; }

        /// <summary>
        /// Number of times this cached result has been reused
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// Metadata about the file analyzed
        /// </summary>
        public CachedFileMetadata FileMetadata { get; set; } = new();

        /// <summary>
        /// Whether this entry is still valid
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Calculate cost savings from cache hits
        /// </summary>
        public double TotalCostSavings => CostInDollars * (HitCount - 1); // Exclude first run
    }

    /// <summary>
    /// Metadata about the cached file
    /// </summary>
    public class CachedFileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
    }
}
