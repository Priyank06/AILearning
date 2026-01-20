using PoC1_LegacyAnalyzer_Web.Models.MultiAgent;

namespace PoC1_LegacyAnalyzer_Web.Services.Validation
{
    /// <summary>
    /// Service for validating AI-generated findings to detect hallucinations and invalid data.
    /// </summary>
    public interface IFindingValidationService
    {
        /// <summary>
        /// Validates a single finding against file content.
        /// </summary>
        /// <param name="finding">The finding to validate</param>
        /// <param name="fileContent">The content of the file referenced in the finding</param>
        /// <param name="fileName">The name of the file</param>
        /// <returns>Validation result with status, errors, and warnings</returns>
        FindingValidationResult ValidateFinding(Finding finding, string? fileContent, string fileName);

        /// <summary>
        /// Validates multiple findings, also checking for contradictions between them.
        /// </summary>
        /// <param name="findings">List of findings to validate</param>
        /// <param name="fileContents">Dictionary mapping file names to their content</param>
        /// <returns>List of validation results, one per finding</returns>
        List<FindingValidationResult> ValidateFindings(List<Finding> findings, Dictionary<string, string> fileContents);
    }
}

