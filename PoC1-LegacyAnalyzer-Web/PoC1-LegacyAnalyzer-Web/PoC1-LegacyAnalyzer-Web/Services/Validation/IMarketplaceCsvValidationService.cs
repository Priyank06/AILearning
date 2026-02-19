using PoC1_LegacyAnalyzer_Web.Models.Marketplace;

namespace PoC1_LegacyAnalyzer_Web.Services.Validation
{
    /// <summary>
    /// Validates a marketplace CSV submission against the Microsoft 365 Certification schema.
    /// </summary>
    public interface IMarketplaceCsvValidationService
    {
        /// <summary>
        /// Validates the supplied field values against the marketplace CSV schema rules.
        /// </summary>
        /// <param name="csvData">
        /// Dictionary keyed by field ID (e.g. "GEN17_aiFunctionality") with the answer as the value.
        /// Unknown field IDs are ignored.
        /// </param>
        /// <returns>A <see cref="MarketplaceCsvValidationResult"/> describing all issues found.</returns>
        MarketplaceCsvValidationResult ValidateCsvData(IReadOnlyDictionary<string, string?> csvData);

        /// <summary>
        /// Returns the full list of field definitions that make up the marketplace CSV schema.
        /// Useful for building UI forms or generating blank CSV templates.
        /// </summary>
        IReadOnlyList<MarketplaceFieldDefinition> GetFieldDefinitions();
    }
}
