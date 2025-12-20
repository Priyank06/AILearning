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
            var pythonStrategy = new PythonPatternDetectionStrategy();
            var multiLangStrategy = new MultiLanguagePatternDetectionStrategy();
            
            _strategies = new Dictionary<string, IPatternDetectionStrategy>
            {
                { "csharp", new CSharpPatternDetectionStrategy() },
                { "python", pythonStrategy },
                { "javascript", multiLangStrategy },
                { "typescript", multiLangStrategy },
                { "java", multiLangStrategy },
                { "go", multiLangStrategy }
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

            var languageKey = language?.ToLower() ?? "unknown";
            
            if (_strategies.TryGetValue(languageKey, out var strategy))
            {
                strategy.DetectPatterns(code, analysis);
                _logger?.LogDebug("Pattern detection completed for language '{Language}': {SecurityCount} security, {PerformanceCount} performance, {ArchitectureCount} architecture findings", 
                    language, analysis.SecurityFindings.Count, analysis.PerformanceFindings.Count, analysis.ArchitectureFindings.Count);
            }
            else
            {
                _logger?.LogWarning("No pattern detection strategy found for language: {Language}, returning empty analysis", language);
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

