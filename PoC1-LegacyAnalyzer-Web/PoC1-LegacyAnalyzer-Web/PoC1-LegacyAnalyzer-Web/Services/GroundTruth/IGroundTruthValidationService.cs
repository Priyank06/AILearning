using PoC1_LegacyAnalyzer_Web.Models.AgentCommunication;
using PoC1_LegacyAnalyzer_Web.Models.GroundTruth;

namespace PoC1_LegacyAnalyzer_Web.Services.GroundTruth
{
    /// <summary>
    /// Service for validating AI analysis results against ground truth datasets
    /// </summary>
    public interface IGroundTruthValidationService
    {
        /// <summary>
        /// Validate AI findings against a ground truth dataset
        /// </summary>
        Task<GroundTruthValidationResult> ValidateAsync(
            TeamAnalysisResult analysisResult,
            GroundTruthDataset groundTruthDataset,
            ValidationConfiguration? configuration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Load a ground truth dataset from file
        /// </summary>
        Task<GroundTruthDataset> LoadDatasetAsync(
            string datasetPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Save a ground truth dataset to file
        /// </summary>
        Task SaveDatasetAsync(
            GroundTruthDataset dataset,
            string datasetPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a new empty ground truth dataset
        /// </summary>
        GroundTruthDataset CreateDataset(string name, string description);

        /// <summary>
        /// Calculate quality metrics from validation result
        /// </summary>
        QualityMetrics CalculateMetrics(GroundTruthValidationResult validationResult);

        /// <summary>
        /// Generate a summary report of validation results
        /// </summary>
        string GenerateSummaryReport(GroundTruthValidationResult validationResult);
    }
}
