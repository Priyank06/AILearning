using Microsoft.AspNetCore.Components.Forms;
using PoC1_LegacyAnalyzer_Web.Models.Determinism;

namespace PoC1_LegacyAnalyzer_Web.Services.Determinism
{
    /// <summary>
    /// Service for measuring determinism of AI analysis by running multiple analyses and comparing results
    /// </summary>
    public interface IDeterminismMeasurementService
    {
        /// <summary>
        /// Measure determinism by running the same analysis multiple times
        /// </summary>
        /// <param name="files">Files to analyze</param>
        /// <param name="businessObjective">Business objective for analysis</param>
        /// <param name="requiredSpecialties">Specialist agents to use</param>
        /// <param name="configuration">Configuration for determinism testing</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Determinism measurement results</returns>
        Task<DeterminismResult> MeasureDeterminismAsync(
            List<IBrowserFile> files,
            string businessObjective,
            List<string> requiredSpecialties,
            DeterminismConfiguration? configuration = null,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Compare two sets of findings to calculate similarity
        /// </summary>
        /// <param name="findings1">First set of findings</param>
        /// <param name="findings2">Second set of findings</param>
        /// <returns>Similarity score (0-100%)</returns>
        double CalculateFindingSimilarity(
            List<PoC1_LegacyAnalyzer_Web.Models.MultiAgent.Finding> findings1,
            List<PoC1_LegacyAnalyzer_Web.Models.MultiAgent.Finding> findings2);

        /// <summary>
        /// Generate a summary report of determinism results
        /// </summary>
        string GenerateSummaryReport(DeterminismResult result);
    }
}
