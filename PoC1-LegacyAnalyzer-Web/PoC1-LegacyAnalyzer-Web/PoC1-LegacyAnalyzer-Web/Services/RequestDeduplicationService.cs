using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for deduplicating requests to prevent duplicate API calls.
    /// </summary>
    public interface IRequestDeduplicationService
    {
        /// <summary>
        /// Generates a fingerprint for a request based on files and configuration.
        /// </summary>
        string GenerateRequestFingerprint(List<IBrowserFile> files, string businessObjective, List<string> selectedAgents);

        /// <summary>
        /// Checks if a request with the given fingerprint was recently processed.
        /// </summary>
        Task<bool> IsDuplicateAsync(string fingerprint);

        /// <summary>
        /// Stores a request fingerprint with its result.
        /// </summary>
        Task StoreRequestAsync<T>(string fingerprint, T result);

        /// <summary>
        /// Retrieves a cached result for a request fingerprint.
        /// </summary>
        Task<T?> GetCachedResultAsync<T>(string fingerprint);
    }

    /// <summary>
    /// Implementation of request deduplication service using in-memory cache.
    /// </summary>
    public class RequestDeduplicationService : IRequestDeduplicationService
    {
        private readonly RequestDeduplicationConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RequestDeduplicationService> _logger;

        public RequestDeduplicationService(
            IOptions<RequestDeduplicationConfiguration> config,
            IMemoryCache cache,
            ILogger<RequestDeduplicationService> logger)
        {
            _config = config?.Value ?? new RequestDeduplicationConfiguration();
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateRequestFingerprint(List<IBrowserFile> files, string businessObjective, List<string> selectedAgents)
        {
            if (!_config.Enabled)
                return string.Empty;

            var fingerprintData = new StringBuilder();

            // Include file metadata (name, size, last modified)
            foreach (var file in files.OrderBy(f => f.Name))
            {
                fingerprintData.Append($"{file.Name}:{file.Size}:{file.LastModified:O}|");
            }

            // Include business objective if configured
            if (_config.IncludeBusinessObjective)
            {
                fingerprintData.Append($"Objective:{businessObjective}|");
            }

            // Include agent selection if configured
            if (_config.IncludeAgentSelection)
            {
                var sortedAgents = selectedAgents.OrderBy(a => a).ToList();
                fingerprintData.Append($"Agents:{string.Join(",", sortedAgents)}|");
            }

            // Generate hash
            var data = fingerprintData.ToString();
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            var fingerprint = Convert.ToBase64String(hashBytes);

            _logger.LogDebug("Generated request fingerprint: {Fingerprint} (from {FileCount} files)", 
                fingerprint.Substring(0, Math.Min(16, fingerprint.Length)), 
                files.Count);

            return fingerprint;
        }

        public async Task<bool> IsDuplicateAsync(string fingerprint)
        {
            if (!_config.Enabled || string.IsNullOrEmpty(fingerprint))
                return false;

            var cacheKey = $"dedup:{fingerprint}";
            var exists = _cache.TryGetValue(cacheKey, out _);
            
            if (exists)
            {
                _logger.LogInformation("Duplicate request detected: {Fingerprint}", 
                    fingerprint.Substring(0, Math.Min(16, fingerprint.Length)));
            }

            return exists;
        }

        public async Task StoreRequestAsync<T>(string fingerprint, T result)
        {
            if (!_config.Enabled || string.IsNullOrEmpty(fingerprint))
                return;

            var cacheKey = $"dedup:{fingerprint}";
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.CacheExpirationSeconds),
                Size = 1 // Each entry counts as 1 unit for size limit
            };

            // Store result
            var resultKey = $"result:{fingerprint}";
            var resultCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.CacheExpirationSeconds),
                Size = 1 // Each entry counts as 1 unit for size limit
            };
            _cache.Set(resultKey, result, resultCacheOptions);

            // Store fingerprint marker
            _cache.Set(cacheKey, true, cacheOptions);

            _logger.LogDebug("Stored request result: {Fingerprint}", 
                fingerprint.Substring(0, Math.Min(16, fingerprint.Length)));
        }

        public async Task<T?> GetCachedResultAsync<T>(string fingerprint)
        {
            if (!_config.Enabled || string.IsNullOrEmpty(fingerprint))
                return default;

            var resultKey = $"result:{fingerprint}";
            if (_cache.TryGetValue(resultKey, out var cachedResult) && cachedResult is T result)
            {
                _logger.LogInformation("Retrieved cached result: {Fingerprint}", 
                    fingerprint.Substring(0, Math.Min(16, fingerprint.Length)));
                return result;
            }

            return default;
        }
    }
}

