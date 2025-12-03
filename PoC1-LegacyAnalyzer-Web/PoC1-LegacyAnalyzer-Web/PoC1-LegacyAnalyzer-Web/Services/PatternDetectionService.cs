using PoC1_LegacyAnalyzer_Web.Models;
using Microsoft.Extensions.Logging;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for detecting code patterns and anti-patterns using strategy pattern for extensibility.
    /// </summary>
    public class PatternDetectionService : IPatternDetectionService
    {
        private readonly ILogger<PatternDetectionService> _logger;
        private readonly Dictionary<string, IPatternDetectionStrategy> _strategies;

        public PatternDetectionService(ILogger<PatternDetectionService> logger)
        {
            _logger = logger;
            _strategies = new Dictionary<string, IPatternDetectionStrategy>
            {
                { "csharp", new CSharpPatternDetectionStrategy() }
            };
        }

        public CodePatternAnalysis DetectPatterns(string code, string language)
        {
            var analysis = new CodePatternAnalysis();

            if (string.IsNullOrWhiteSpace(code))
            {
                _logger?.LogWarning("DetectPatterns called with null or empty code");
                return analysis;
            }

            if (!string.Equals(language, "csharp", StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("Pattern detection for language '{Language}' not supported, returning empty analysis", language);
                return analysis;
            }

            if (_strategies.TryGetValue(language.ToLower(), out var strategy))
            {
                strategy.DetectPatterns(code, analysis);
            }
            else
            {
                _logger?.LogWarning("No pattern detection strategy found for language: {Language}", language);
            }

            return analysis;
        }

        public List<string> GetSupportedLanguages()
        {
            return _strategies.Keys.ToList();
        }

        /// <summary>
        /// Registers a new pattern detection strategy for a language.
        /// Allows extensibility for new pattern types.
        /// </summary>
        public void RegisterStrategy(string language, IPatternDetectionStrategy strategy)
        {
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            _strategies[language.ToLower()] = strategy;
            _logger?.LogInformation("Registered pattern detection strategy for language: {Language}", language);
        }
    }
}

