using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for calculating code complexity metrics using local static analysis.
    /// </summary>
    public interface IComplexityCalculationService
    {
        /// <summary>
        /// Calculates code complexity metrics (cyclomatic, lines of code, etc.).
        /// </summary>
        /// <param name="code">The source code to analyze.</param>
        /// <param name="language">The language of the source code (e.g., "csharp").</param>
        /// <returns>A <see cref="ComplexityMetrics"/> object containing calculated metrics.</returns>
        ComplexityMetrics CalculateComplexity(string code, string language);
    }
}

