namespace PoC1_LegacyAnalyzer_Web.Models.Marketplace
{
    /// <summary>
    /// Severity level of a marketplace CSV validation issue.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>The field value is invalid and must be corrected before submission.</summary>
        Error,

        /// <summary>The field value is suspicious or incomplete but may be intentional.</summary>
        Warning
    }

    /// <summary>
    /// Describes a single validation issue found for a marketplace CSV field.
    /// </summary>
    public class MarketplaceCsvFieldError
    {
        /// <summary>The field identifier, e.g. "GEN17_aiFunctionality".</summary>
        public string FieldId { get; init; } = string.Empty;

        /// <summary>Human-readable question text for the failing field.</summary>
        public string Question { get; init; } = string.Empty;

        /// <summary>The value that was supplied (may be null/empty).</summary>
        public string? ProvidedValue { get; init; }

        /// <summary>Description of the validation failure.</summary>
        public string ErrorMessage { get; init; } = string.Empty;

        /// <summary>Whether this is an error that blocks submission or just a warning.</summary>
        public ValidationSeverity Severity { get; init; }
    }

    /// <summary>
    /// Aggregated result of validating all fields in a marketplace CSV submission.
    /// </summary>
    public class MarketplaceCsvValidationResult
    {
        /// <summary>True when no errors (warnings are still allowed) were found.</summary>
        public bool IsValid { get; init; }

        /// <summary>All validation issues discovered during the validation run.</summary>
        public IReadOnlyList<MarketplaceCsvFieldError> Issues { get; init; } = [];

        /// <summary>Total number of fields that were evaluated.</summary>
        public int TotalFieldsValidated { get; init; }

        /// <summary>Number of distinct fields that have at least one error.</summary>
        public int FieldsWithErrors { get; init; }

        /// <summary>Number of distinct fields that have at least one warning (but no errors).</summary>
        public int FieldsWithWarnings { get; init; }

        /// <summary>Convenience shortcut to only error-level issues.</summary>
        public IEnumerable<MarketplaceCsvFieldError> Errors =>
            Issues.Where(i => i.Severity == ValidationSeverity.Error);

        /// <summary>Convenience shortcut to only warning-level issues.</summary>
        public IEnumerable<MarketplaceCsvFieldError> Warnings =>
            Issues.Where(i => i.Severity == ValidationSeverity.Warning);
    }
}
