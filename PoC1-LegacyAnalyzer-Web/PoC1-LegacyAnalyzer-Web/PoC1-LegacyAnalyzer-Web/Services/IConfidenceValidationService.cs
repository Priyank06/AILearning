using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for validating and normalizing confidence scores and explainability data.
    /// </summary>
    public interface IConfidenceValidationService
    {
        /// <summary>
        /// Validates and normalizes explainability data for a finding.
        /// </summary>
        ExplainableFinding ValidateAndNormalize(ExplainableFinding? explainability);

        /// <summary>
        /// Calculates confidence score from breakdown if not provided.
        /// </summary>
        int CalculateConfidenceFromBreakdown(ConfidenceBreakdown breakdown);

        /// <summary>
        /// Validates that confidence breakdown components are reasonable.
        /// </summary>
        bool ValidateConfidenceBreakdown(ConfidenceBreakdown breakdown);
    }
}

