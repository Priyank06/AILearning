using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for detecting legacy-specific anti-patterns in code.
    /// </summary>
    public interface ILegacyPatternDetectionService
    {
        /// <summary>
        /// Detects legacy patterns in the provided code.
        /// </summary>
        /// <param name="code">The source code to analyze</param>
        /// <param name="language">The programming language</param>
        /// <param name="context">Optional context about the file (age, framework, etc.)</param>
        /// <returns>Legacy pattern detection results</returns>
        LegacyPatternResult DetectLegacyPatterns(string code, string language, LegacyContext? context = null);

        /// <summary>
        /// Gets legacy indicators for a file based on its metadata.
        /// </summary>
        LegacyIndicators GetLegacyIndicators(LegacyContext context);
    }
}

