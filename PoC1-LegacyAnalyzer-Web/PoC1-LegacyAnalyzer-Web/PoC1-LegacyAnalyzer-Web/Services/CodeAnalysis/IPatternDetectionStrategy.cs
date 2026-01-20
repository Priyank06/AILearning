using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.CodeAnalysis
{
    /// <summary>
    /// Strategy interface for pattern detection algorithms.
    /// Allows extensibility for new pattern types.
    /// </summary>
    public interface IPatternDetectionStrategy
    {
        /// <summary>
        /// Detects patterns in the given code.
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="analysis">The CodePatternAnalysis object to populate.</param>
        void DetectPatterns(string code, CodePatternAnalysis analysis);
    }
}

