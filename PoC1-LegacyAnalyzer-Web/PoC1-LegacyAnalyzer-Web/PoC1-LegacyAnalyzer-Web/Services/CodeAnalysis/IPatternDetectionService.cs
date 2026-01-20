using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Service for detecting code patterns and anti-patterns using local static analysis.
    /// Uses strategy pattern for extensibility.
    /// </summary>
    public interface IPatternDetectionService
    {
        /// <summary>
        /// Detects common code patterns and anti-patterns in source code.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="language">The language of the source code (e.g., "csharp").</param>
        /// <returns>A <see cref="CodePatternAnalysis"/> object containing detected patterns.</returns>
        CodePatternAnalysis DetectPatterns(string code, string language);

        /// <summary>
        /// Gets the list of supported languages for pattern detection.
        /// </summary>
        /// <returns>A list of supported language strings.</returns>
        List<string> GetSupportedLanguages();
    }
}

