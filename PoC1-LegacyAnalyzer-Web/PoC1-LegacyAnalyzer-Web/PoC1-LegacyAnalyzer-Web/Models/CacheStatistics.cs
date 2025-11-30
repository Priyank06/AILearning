namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Statistics for file preprocessing cache performance and usage.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Total number of cache hits (successful cache retrievals).
        /// </summary>
        public long TotalHits { get; set; }

        /// <summary>
        /// Total number of cache misses (cache lookups that required new processing).
        /// </summary>
        public long TotalMisses { get; set; }

        /// <summary>
        /// Cache hit rate as a percentage (0-100).
        /// Calculated as (TotalHits / TotalRequests) * 100.
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// Current number of items in the cache.
        /// </summary>
        public int CachedItemCount { get; set; }

        /// <summary>
        /// Total number of cache requests (hits + misses).
        /// </summary>
        public long TotalRequests => TotalHits + TotalMisses;

        /// <summary>
        /// Cache time-to-live in minutes.
        /// </summary>
        public int CacheTTLMinutes { get; set; }

        /// <summary>
        /// Maximum cache size limit.
        /// </summary>
        public int MaxCacheSize { get; set; }
    }
}

