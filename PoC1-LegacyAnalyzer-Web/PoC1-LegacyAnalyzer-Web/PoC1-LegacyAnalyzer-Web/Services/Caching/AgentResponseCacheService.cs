using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models.Caching;
using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services.Caching
{
    /// <summary>
    /// Implementation of agent response caching using IMemoryCache
    /// Reduces API costs by caching agent responses based on file content + agent + objective hash
    /// </summary>
    public class AgentResponseCacheService : IAgentResponseCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<AgentResponseCacheService> _logger;
        private readonly AgentCacheConfiguration _config;
        private readonly object _statsLock = new();

        // In-memory statistics tracking
        private int _totalHits = 0;
        private int _totalMisses = 0;

        // Cache key prefix for cache entries
        private const string CacheKeyPrefix = "agent_response:";
        private const string StatsKeyPrefix = "agent_stats:";
        private const string AllEntriesKey = "all_entries_list";

        public AgentResponseCacheService(
            IMemoryCache cache,
            ILogger<AgentResponseCacheService> logger,
            IOptions<AgentCacheConfiguration> config)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? new AgentCacheConfiguration();
        }

        public async Task<SpecialistAnalysisResult?> GetCachedResponseAsync(
            string agentName,
            string fileContent,
            string businessObjective,
            string model)
        {
            var cacheKey = GenerateCacheKey(agentName, fileContent, businessObjective, model);

            if (_cache.TryGetValue<AgentResponseCacheEntry>(cacheKey, out var entry))
            {
                // Check if expired
                if (entry.IsExpired)
                {
                    _logger.LogDebug("Cache entry expired for key: {CacheKey}", cacheKey);
                    _cache.Remove(cacheKey);
                    IncrementMisses();
                    return null;
                }

                // Increment hit count
                entry.HitCount++;
                _cache.Set(cacheKey, entry, GetCacheOptions());

                IncrementHits();
                _logger.LogInformation(
                    "Cache HIT for agent {AgentName}. Hit count: {HitCount}, Cost savings: ${CostSavings:F4}",
                    agentName,
                    entry.HitCount,
                    entry.TotalCostSavings);

                return await Task.FromResult(entry.AnalysisResult);
            }

            IncrementMisses();
            _logger.LogDebug("Cache MISS for agent {AgentName}", agentName);
            return null;
        }

        public async Task CacheResponseAsync(
            string agentName,
            string specialty,
            string fileContent,
            string businessObjective,
            string model,
            SpecialistAnalysisResult analysisResult,
            int tokenCount,
            double costInDollars,
            string fileName = "",
            string language = "")
        {
            var cacheKey = GenerateCacheKey(agentName, fileContent, businessObjective, model);
            var contentHash = ComputeHash(fileContent);
            var objectiveHash = ComputeHash(businessObjective);

            var entry = new AgentResponseCacheEntry
            {
                CacheKey = cacheKey,
                AgentName = agentName,
                Specialty = specialty,
                AnalysisResult = analysisResult,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_config.CacheExpirationMinutes),
                FileContentHash = contentHash,
                BusinessObjectiveHash = objectiveHash,
                Model = model,
                TokenCount = tokenCount,
                CostInDollars = costInDollars,
                HitCount = 1, // First use
                FileMetadata = new CachedFileMetadata
                {
                    FileName = fileName,
                    Language = language,
                    LineCount = fileContent.Split('\n').Length
                }
            };

            _cache.Set(cacheKey, entry, GetCacheOptions());

            // Track this entry in the all entries list
            AddToAllEntries(cacheKey);

            _logger.LogInformation(
                "Cached response for agent {AgentName}, model: {Model}, tokens: {TokenCount}, cost: ${Cost:F4}",
                agentName, model, tokenCount, costInDollars);

            await Task.CompletedTask;
        }

        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            var stats = new CacheStatistics
            {
                TotalHits = _totalHits,
                TotalMisses = _totalMisses
            };

            var allEntries = await GetAllEntriesAsync();
            stats.TotalEntries = allEntries.Count;
            stats.ExpiredEntries = allEntries.Count(e => e.IsExpired);

            // Calculate cost savings and tokens saved
            foreach (var entry in allEntries)
            {
                if (!entry.IsExpired)
                {
                    stats.TotalCostSavings += entry.TotalCostSavings;
                    stats.TotalTokensSaved += entry.TokenCount * (entry.HitCount - 1);
                }
            }

            // Group by agent
            var groupedByAgent = allEntries
                .Where(e => !e.IsExpired)
                .GroupBy(e => e.AgentName);

            foreach (var group in groupedByAgent)
            {
                stats.StatsByAgent[group.Key] = new AgentCacheStatistics
                {
                    AgentName = group.Key,
                    Entries = group.Count(),
                    Hits = group.Sum(e => e.HitCount - 1), // Exclude first run
                    CostSavings = group.Sum(e => e.TotalCostSavings),
                    TokensSaved = group.Sum(e => e.TokenCount * (e.HitCount - 1))
                };
            }

            return stats;
        }

        public async Task ClearExpiredEntriesAsync()
        {
            var allEntries = await GetAllEntriesAsync();
            var expiredKeys = allEntries.Where(e => e.IsExpired).Select(e => e.CacheKey).ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            // Rebuild all entries list
            RebuildAllEntriesList(allEntries.Where(e => !e.IsExpired).Select(e => e.CacheKey).ToList());

            _logger.LogInformation("Cleared {Count} expired cache entries", expiredKeys.Count);
        }

        public async Task ClearAllAsync()
        {
            var allEntries = await GetAllEntriesAsync();

            foreach (var entry in allEntries)
            {
                _cache.Remove(entry.CacheKey);
            }

            _cache.Remove(AllEntriesKey);

            lock (_statsLock)
            {
                _totalHits = 0;
                _totalMisses = 0;
            }

            _logger.LogInformation("Cleared all cache entries ({Count} total)", allEntries.Count);
        }

        public async Task<List<AgentResponseCacheEntry>> GetAllEntriesAsync()
        {
            var entries = new List<AgentResponseCacheEntry>();

            if (_cache.TryGetValue<List<string>>(AllEntriesKey, out var cacheKeys))
            {
                foreach (var key in cacheKeys)
                {
                    if (_cache.TryGetValue<AgentResponseCacheEntry>(key, out var entry))
                    {
                        entries.Add(entry);
                    }
                }
            }

            return await Task.FromResult(entries);
        }

        #region Private Helpers

        private string GenerateCacheKey(string agentName, string fileContent, string businessObjective, string model)
        {
            // Create a deterministic key based on inputs
            var combined = $"{agentName}|{model}|{ComputeHash(fileContent)}|{ComputeHash(businessObjective)}";
            var hash = ComputeHash(combined);
            return $"{CacheKeyPrefix}{hash}";
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private MemoryCacheEntryOptions GetCacheOptions()
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.CacheExpirationMinutes),
                SlidingExpiration = _config.UseSlidingExpiration
                    ? TimeSpan.FromMinutes(_config.SlidingExpirationMinutes)
                    : null,
                Size = 1 // Each entry counts as 1 unit for cache size limit
            };
        }

        private void AddToAllEntries(string cacheKey)
        {
            var allKeys = _cache.Get<List<string>>(AllEntriesKey) ?? new List<string>();
            if (!allKeys.Contains(cacheKey))
            {
                allKeys.Add(cacheKey);
                _cache.Set(AllEntriesKey, allKeys, new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.NeverRemove
                });
            }
        }

        private void RebuildAllEntriesList(List<string> cacheKeys)
        {
            _cache.Set(AllEntriesKey, cacheKeys, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            });
        }

        private void IncrementHits()
        {
            lock (_statsLock)
            {
                _totalHits++;
            }
        }

        private void IncrementMisses()
        {
            lock (_statsLock)
            {
                _totalMisses++;
            }
        }

        #endregion
    }

    /// <summary>
    /// Configuration for agent response caching
    /// </summary>
    public class AgentCacheConfiguration
    {
        /// <summary>
        /// How long to cache responses (in minutes)
        /// Default: 60 minutes
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Whether to use sliding expiration (extends TTL on each access)
        /// Default: true
        /// </summary>
        public bool UseSlidingExpiration { get; set; } = true;

        /// <summary>
        /// Sliding expiration window (in minutes)
        /// Default: 30 minutes
        /// </summary>
        public int SlidingExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// Maximum cache size (number of entries)
        /// Default: 1000
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// Whether caching is enabled
        /// Default: true
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
