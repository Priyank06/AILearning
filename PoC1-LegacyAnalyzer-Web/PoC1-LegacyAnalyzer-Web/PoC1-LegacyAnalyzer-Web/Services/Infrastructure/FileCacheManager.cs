using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Service for managing file metadata caching with thread-safe operations.
    /// </summary>
    public class FileCacheManager : IFileCacheManager
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<FileCacheManager> _logger;
        private readonly FilePreProcessingOptions _options;

        // Cache statistics (thread-safe)
        private long _cacheHits = 0;
        private long _cacheMisses = 0;
        private readonly object _statsLock = new object();

        // Thread-safe cache key tracking for cache management
        private readonly ConcurrentDictionary<string, object> _cacheKeys = new ConcurrentDictionary<string, object>();

        public FileCacheManager(
            IMemoryCache cache,
            ILogger<FileCacheManager> logger,
            IOptions<FilePreProcessingOptions> options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger;
            _options = options?.Value ?? new FilePreProcessingOptions();
        }

        public bool TryGetCached(string cacheKey, out FileMetadata? metadata)
        {
            if (!_options.EnableCaching)
            {
                metadata = null;
                return false;
            }

            if (_cache.TryGetValue(cacheKey, out FileMetadata? cached) && cached != null)
            {
                lock (_statsLock)
                {
                    _cacheHits++;
                }
                _logger?.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                metadata = cached;
                return true;
            }

            lock (_statsLock)
            {
                _cacheMisses++;
            }
            _logger?.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            metadata = null;
            return false;
        }

        public void SetCached(string cacheKey, FileMetadata metadata, int ttlMinutes)
        {
            if (!_options.EnableCaching)
                return;

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes),
                Size = 1
            };

            // Register callback to remove from tracking when cache entry expires
            cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (key is string keyStr)
                {
                    _cacheKeys.TryRemove(keyStr, out _);
                }
            });

            // Apply size limit if configured
            if (_options.MaxCacheSize > 0)
            {
                cacheOptions.Size = 1;
            }

            _cache.Set(cacheKey, metadata, cacheOptions);
            _cacheKeys.TryAdd(cacheKey, null);

            _logger?.LogDebug("Cached metadata for key: {CacheKey} (TTL: {TTLMinutes} minutes)", cacheKey, ttlMinutes);
        }

        public CacheStatistics GetCacheStatistics()
        {
            long hits, misses;
            int cachedItemCount;

            // Thread-safe read of statistics
            lock (_statsLock)
            {
                hits = _cacheHits;
                misses = _cacheMisses;
            }

            // Get current cache item count (thread-safe)
            cachedItemCount = _cacheKeys.Count;

            var total = hits + misses;
            var hitRate = total > 0 ? (double)hits / total * 100.0 : 0.0;
            var missRate = total > 0 ? (double)misses / total * 100.0 : 0.0;
            var cacheUtilization = _options.MaxCacheSize > 0
                ? (double)cachedItemCount / _options.MaxCacheSize * 100.0
                : 0.0;

            _logger?.LogInformation(
                "Cache performance statistics - Hits: {Hits} ({HitRate:F2}%), Misses: {Misses} ({MissRate:F2}%), " +
                "Total Requests: {TotalRequests}, Cached Items: {CachedItemCount}/{MaxCacheSize} ({CacheUtilization:F1}% utilization), TTL: {TTLMinutes}min",
                hits, hitRate, misses, missRate, total, cachedItemCount, _options.MaxCacheSize, cacheUtilization, _options.CacheTTLMinutes);

            return new CacheStatistics
            {
                TotalHits = hits,
                TotalMisses = misses,
                HitRate = Math.Round(hitRate, 2),
                CachedItemCount = cachedItemCount,
                CacheTTLMinutes = _options.CacheTTLMinutes,
                MaxCacheSize = _options.MaxCacheSize
            };
        }

        public int ClearCache()
        {
            int clearedCount = 0;

            // Get all cache keys (thread-safe snapshot)
            var keysToRemove = _cacheKeys.Keys.ToList();

            // Remove each cache entry (thread-safe)
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
                clearedCount++;
            }

            _logger?.LogInformation("Cleared entire cache. Removed {ClearedCount} entries.", clearedCount);
            return clearedCount;
        }

        public int ClearCacheForFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger?.LogWarning("ClearCacheForFile called with null or empty fileName.");
                return 0;
            }

            int clearedCount = 0;

            // Find all cache keys matching the file name (thread-safe)
            var keysToRemove = _cacheKeys.Keys
                .Where(key => key.Contains(fileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Remove matching cache entries (thread-safe)
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
                clearedCount++;
            }

            if (clearedCount > 0)
            {
                _logger?.LogInformation("Cleared cache for file '{FileName}'. Removed {ClearedCount} entries.", fileName, clearedCount);
            }
            else
            {
                _logger?.LogDebug("No cache entries found for file '{FileName}'.", fileName);
            }

            return clearedCount;
        }
    }
}

