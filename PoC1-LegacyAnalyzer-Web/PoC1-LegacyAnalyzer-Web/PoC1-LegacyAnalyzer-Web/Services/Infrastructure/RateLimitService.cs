using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Collections.Concurrent;

namespace PoC1_LegacyAnalyzer_Web.Services.Infrastructure
{
    /// <summary>
    /// Service for tracking and enforcing rate limits using sliding window algorithm.
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks if a request is allowed for the given client identifier.
        /// </summary>
        /// <param name="clientId">Unique identifier for the client (IP address, user ID, etc.)</param>
        /// <returns>True if request is allowed, false if rate limit exceeded</returns>
        bool IsRequestAllowed(string clientId);

        /// <summary>
        /// Gets the number of remaining requests for the client in the current window.
        /// </summary>
        /// <param name="clientId">Unique identifier for the client</param>
        /// <returns>Number of remaining requests</returns>
        int GetRemainingRequests(string clientId);

        /// <summary>
        /// Gets the time until the rate limit window resets (in seconds).
        /// </summary>
        /// <param name="clientId">Unique identifier for the client</param>
        /// <returns>Seconds until window resets</returns>
        int GetResetTimeSeconds(string clientId);

        /// <summary>
        /// Records a request for the given client.
        /// </summary>
        /// <param name="clientId">Unique identifier for the client</param>
        void RecordRequest(string clientId);
    }

    /// <summary>
    /// Implementation of rate limiting using sliding window algorithm with in-memory cache.
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitService> _logger;
        private readonly RateLimitConfiguration _config;
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public RateLimitService(
            IMemoryCache cache,
            ILogger<RateLimitService> logger,
            IOptions<RateLimitConfiguration> config)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsRequestAllowed(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return false;

            var cacheKey = GetCacheKey(clientId);
            var lockObject = _locks.GetOrAdd(clientId, _ => new object());

            lock (lockObject)
            {
                var window = GetOrCreateWindow(cacheKey);
                var now = DateTime.UtcNow;

                // Remove old entries outside the window
                window.Requests.RemoveAll(r => (now - r).TotalSeconds > _config.WindowSizeSeconds);

                // Check if limit exceeded
                if (window.Requests.Count >= _config.MaxRequestsPerWindow)
                {
                    _logger.LogWarning(
                        "Rate limit exceeded for client {ClientId}. Requests in window: {RequestCount}/{MaxRequests}",
                        clientId,
                        window.Requests.Count,
                        _config.MaxRequestsPerWindow);
                    return false;
                }

                // Record the request
                window.Requests.Add(now);
                
                // Set cache entry with size (required when SizeLimit is configured)
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.WindowSizeSeconds * 2),
                    Size = 1 // Each rate limit window counts as 1 unit
                };
                _cache.Set(cacheKey, window, cacheOptions);

                return true;
            }
        }

        public int GetRemainingRequests(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return 0;

            var cacheKey = GetCacheKey(clientId);
            var window = GetOrCreateWindow(cacheKey);
            var now = DateTime.UtcNow;

            // Remove old entries
            window.Requests.RemoveAll(r => (now - r).TotalSeconds > _config.WindowSizeSeconds);

            return Math.Max(0, _config.MaxRequestsPerWindow - window.Requests.Count);
        }

        public int GetResetTimeSeconds(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return 0;

            var cacheKey = GetCacheKey(clientId);
            var window = GetOrCreateWindow(cacheKey);
            var now = DateTime.UtcNow;

            // Remove old entries
            window.Requests.RemoveAll(r => (now - r).TotalSeconds > _config.WindowSizeSeconds);

            if (window.Requests.Count == 0)
                return 0;

            // Find the oldest request in the window
            var oldestRequest = window.Requests.Min();
            var resetTime = oldestRequest.AddSeconds(_config.WindowSizeSeconds);
            var secondsUntilReset = (int)Math.Ceiling((resetTime - now).TotalSeconds);

            return Math.Max(0, secondsUntilReset);
        }

        public void RecordRequest(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return;

            var cacheKey = GetCacheKey(clientId);
            var lockObject = _locks.GetOrAdd(clientId, _ => new object());

            lock (lockObject)
            {
                var window = GetOrCreateWindow(cacheKey);
                window.Requests.Add(DateTime.UtcNow);
                
                // Set cache entry with size (required when SizeLimit is configured)
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.WindowSizeSeconds * 2),
                    Size = 1 // Each rate limit window counts as 1 unit
                };
                _cache.Set(cacheKey, window, cacheOptions);
            }
        }

        private string GetCacheKey(string clientId)
        {
            return $"ratelimit:{clientId}";
        }

        private RateLimitWindow GetOrCreateWindow(string cacheKey)
        {
            if (_cache.TryGetValue<RateLimitWindow>(cacheKey, out var window))
            {
                return window;
            }

            window = new RateLimitWindow
            {
                ClientId = cacheKey,
                Requests = new List<DateTime>()
            };

            // Set cache entry with size (required when SizeLimit is configured)
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_config.WindowSizeSeconds * 2),
                Size = 1 // Each rate limit window counts as 1 unit
            };
            _cache.Set(cacheKey, window, cacheOptions);
            return window;
        }

        private class RateLimitWindow
        {
            public string ClientId { get; set; } = string.Empty;
            public List<DateTime> Requests { get; set; } = new();
        }
    }
}

