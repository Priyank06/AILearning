using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Service for managing file metadata caching.
    /// </summary>
    public interface IFileCacheManager
    {
        /// <summary>
        /// Gets cache statistics including hit/miss rates and current cache size.
        /// </summary>
        /// <returns>A <see cref="CacheStatistics"/> object containing cache performance metrics.</returns>
        CacheStatistics GetCacheStatistics();

        /// <summary>
        /// Clears all cached file metadata from the cache.
        /// </summary>
        /// <returns>The number of cache entries that were cleared.</returns>
        int ClearCache();

        /// <summary>
        /// Clears cached metadata for a specific file by name.
        /// </summary>
        /// <param name="fileName">The name of the file to remove from cache.</param>
        /// <returns>The number of cache entries that were cleared.</returns>
        int ClearCacheForFile(string fileName);

        /// <summary>
        /// Tries to get cached metadata for a file.
        /// </summary>
        /// <param name="cacheKey">The cache key for the file.</param>
        /// <param name="metadata">The cached metadata if found.</param>
        /// <returns>True if cached metadata was found, false otherwise.</returns>
        bool TryGetCached(string cacheKey, out FileMetadata? metadata);

        /// <summary>
        /// Caches file metadata with TTL.
        /// </summary>
        /// <param name="cacheKey">The cache key for the file.</param>
        /// <param name="metadata">The metadata to cache.</param>
        /// <param name="ttlMinutes">Time to live in minutes.</param>
        void SetCached(string cacheKey, FileMetadata metadata, int ttlMinutes);
    }
}

