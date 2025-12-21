namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for request deduplication to prevent duplicate API calls.
    /// </summary>
    public class RequestDeduplicationConfiguration
    {
        /// <summary>
        /// Enable or disable request deduplication.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Cache expiration time in seconds.
        /// </summary>
        public int CacheExpirationSeconds { get; set; } = 300; // 5 minutes

        /// <summary>
        /// Maximum cache size (number of cached requests).
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// Whether to include file content hash in fingerprint.
        /// </summary>
        public bool IncludeFileContentHash { get; set; } = true;

        /// <summary>
        /// Whether to include business objective in fingerprint.
        /// </summary>
        public bool IncludeBusinessObjective { get; set; } = true;

        /// <summary>
        /// Whether to include agent selection in fingerprint.
        /// </summary>
        public bool IncludeAgentSelection { get; set; } = true;
    }
}

