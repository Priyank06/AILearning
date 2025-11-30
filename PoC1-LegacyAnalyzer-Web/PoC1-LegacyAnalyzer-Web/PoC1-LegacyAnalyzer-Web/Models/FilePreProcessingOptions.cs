using System.ComponentModel.DataAnnotations;

namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration options for file preprocessing service.
    /// Controls performance parameters, caching behavior, and processing limits.
    /// Bound from appsettings.json "FilePreProcessing" section via IOptions pattern.
    /// </summary>
    public class FilePreProcessingOptions
    {
        /// <summary>
        /// Maximum number of files to process concurrently in parallel operations.
        /// Higher values increase throughput but consume more system resources.
        /// Default: 5 files.
        /// </summary>
        [Range(1, 50, ErrorMessage = "MaxConcurrentFiles must be between 1 and 50")]
        public int MaxConcurrentFiles { get; set; } = 5;

        /// <summary>
        /// Cache time-to-live in minutes. Metadata is cached for this duration to avoid redundant Roslyn parsing.
        /// Longer TTL improves performance but may serve stale data if files change frequently.
        /// Default: 60 minutes (1 hour).
        /// </summary>
        [Range(1, 1440, ErrorMessage = "CacheTTLMinutes must be between 1 and 1440 (24 hours)")]
        public int CacheTTLMinutes { get; set; } = 60;

        /// <summary>
        /// Maximum number of cache entries. Used to prevent unbounded memory growth.
        /// When limit is reached, least recently used entries are evicted.
        /// Default: 1000 entries.
        /// </summary>
        [Range(1, 100000, ErrorMessage = "MaxCacheSize must be between 1 and 100000")]
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// Minimum cyclomatic complexity threshold for routing files to performance specialist agents.
        /// Files with complexity >= this value are considered for performance analysis.
        /// Default: 15.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "MinComplexityThreshold must be between 1 and 1000")]
        public int MinComplexityThreshold { get; set; } = 15;

        /// <summary>
        /// Enables or disables pattern detection (security, performance, architecture anti-patterns).
        /// When disabled, pattern detection is skipped to improve performance, but analysis quality is reduced.
        /// Default: true (enabled).
        /// </summary>
        public bool EnablePatternDetection { get; set; } = true;

        /// <summary>
        /// Enables or disables metadata caching.
        /// When disabled, all files are reprocessed on every request, increasing CPU usage but ensuring fresh data.
        /// Default: true (enabled).
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Maximum file size in megabytes that can be processed.
        /// Files larger than this limit are rejected to prevent memory issues and performance degradation.
        /// Default: 5 MB.
        /// </summary>
        [Range(1, 100, ErrorMessage = "MaxFileSizeMB must be between 1 and 100")]
        public int MaxFileSizeMB { get; set; } = 5;

        /// <summary>
        /// Pattern detection configuration. Controls which categories of patterns are detected.
        /// </summary>
        public PatternDetectionOptions PatternDetection { get; set; } = new PatternDetectionOptions();

        /// <summary>
        /// Performance monitoring and benchmarking configuration.
        /// </summary>
        public PerformanceOptions Performance { get; set; } = new PerformanceOptions();

        /// <summary>
        /// Validates the configuration for consistency and correctness.
        /// Checks that all values are within acceptable ranges and that related settings are compatible.
        /// </summary>
        /// <param name="options">The FilePreProcessingOptions instance to validate.</param>
        /// <returns>A tuple containing (isValid: bool, errorMessage: string).</returns>
        /// <remarks>
        /// Use this method during application startup or configuration validation to ensure
        /// FilePreProcessingOptions are properly configured before use.
        /// <para>Example usage:</para>
        /// <code>
        /// var (isValid, error) = FilePreProcessingOptions.Validate(options);
        /// if (!isValid) throw new InvalidOperationException(error);
        /// </code>
        /// </remarks>
        public static (bool IsValid, string ErrorMessage) Validate(FilePreProcessingOptions options)
        {
            if (options == null)
            {
                return (false, "FilePreProcessingOptions cannot be null");
            }

            // Validate MaxConcurrentFiles
            if (options.MaxConcurrentFiles < 1 || options.MaxConcurrentFiles > 50)
            {
                return (false, $"MaxConcurrentFiles must be between 1 and 50, but was {options.MaxConcurrentFiles}");
            }

            // Validate CacheTTLMinutes
            if (options.CacheTTLMinutes < 1 || options.CacheTTLMinutes > 1440)
            {
                return (false, $"CacheTTLMinutes must be between 1 and 1440 (24 hours), but was {options.CacheTTLMinutes}");
            }

            // Validate MaxCacheSize
            if (options.MaxCacheSize < 1 || options.MaxCacheSize > 100000)
            {
                return (false, $"MaxCacheSize must be between 1 and 100000, but was {options.MaxCacheSize}");
            }

            // Validate MinComplexityThreshold
            if (options.MinComplexityThreshold < 1 || options.MinComplexityThreshold > 1000)
            {
                return (false, $"MinComplexityThreshold must be between 1 and 1000, but was {options.MinComplexityThreshold}");
            }

            // Validate MaxFileSizeMB
            if (options.MaxFileSizeMB < 1 || options.MaxFileSizeMB > 100)
            {
                return (false, $"MaxFileSizeMB must be between 1 and 100, but was {options.MaxFileSizeMB}");
            }

            // Cross-property validation: If caching is enabled, cache size should be reasonable
            if (options.EnableCaching && options.MaxCacheSize < 10)
            {
                return (false, $"MaxCacheSize should be at least 10 when caching is enabled, but was {options.MaxCacheSize}");
            }

            // Cross-property validation: MaxConcurrentFiles should not exceed MaxCacheSize significantly
            if (options.MaxConcurrentFiles > options.MaxCacheSize / 10)
            {
                return (false, $"MaxConcurrentFiles ({options.MaxConcurrentFiles}) is too high relative to MaxCacheSize ({options.MaxCacheSize}). Consider increasing MaxCacheSize or reducing MaxConcurrentFiles.");
            }

            // Validate nested PatternDetection configuration
            if (options.PatternDetection != null)
            {
                // Pattern detection validation is optional - if EnablePatternDetection is false, these don't matter
                if (options.EnablePatternDetection && !options.PatternDetection.EnableSecurityPatterns && 
                    !options.PatternDetection.EnablePerformancePatterns && 
                    !options.PatternDetection.EnableArchitecturePatterns)
                {
                    return (false, "EnablePatternDetection is true but all pattern categories are disabled. Enable at least one pattern category.");
                }
            }

            return (true, string.Empty);
        }
    }

    /// <summary>
    /// Configuration options for pattern detection categories.
    /// Allows fine-grained control over which pattern types are detected.
    /// </summary>
    public class PatternDetectionOptions
    {
        /// <summary>
        /// Enables or disables security pattern detection (SQL injection, XSS, hardcoded credentials, etc.).
        /// When disabled, security findings are not included in analysis results.
        /// Default: true (enabled).
        /// </summary>
        public bool EnableSecurityPatterns { get; set; } = true;

        /// <summary>
        /// Enables or disables performance pattern detection (N+1 queries, blocking delays, sync-over-async, etc.).
        /// When disabled, performance findings are not included in analysis results.
        /// Default: true (enabled).
        /// </summary>
        public bool EnablePerformancePatterns { get; set; } = true;

        /// <summary>
        /// Enables or disables architecture pattern detection (god classes, long methods, deep inheritance, etc.).
        /// When disabled, architecture findings are not included in analysis results.
        /// Default: true (enabled).
        /// </summary>
        public bool EnableArchitecturePatterns { get; set; } = true;
    }

    /// <summary>
    /// Configuration options for performance monitoring and benchmarking.
    /// Controls detailed metrics collection and benchmarking features.
    /// </summary>
    public class PerformanceOptions
    {
        /// <summary>
        /// Enables detailed performance metrics logging for each operation.
        /// When enabled, logs include per-file timing, throughput, and speedup calculations.
        /// When disabled, only summary metrics are logged.
        /// Default: false (disabled) for production, true for development.
        /// </summary>
        public bool LogDetailedMetrics { get; set; } = false;

        /// <summary>
        /// Enables benchmarking mode for performance analysis.
        /// When enabled, collects additional timing data and performance statistics.
        /// Useful for performance tuning and optimization validation.
        /// Default: false (disabled).
        /// </summary>
        public bool EnableBenchmarking { get; set; } = false;
    }
}

