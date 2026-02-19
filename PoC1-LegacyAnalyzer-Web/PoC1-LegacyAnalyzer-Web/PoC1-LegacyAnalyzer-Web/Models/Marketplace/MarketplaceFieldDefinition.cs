namespace PoC1_LegacyAnalyzer_Web.Models.Marketplace
{
    /// <summary>
    /// Describes the validation schema for a single field in the marketplace CSV submission.
    /// </summary>
    public class MarketplaceFieldDefinition
    {
        /// <summary>
        /// The unique field identifier, e.g. "GEN17_aiFunctionality". Matches the parenthesised ID in the CSV question.
        /// </summary>
        public string FieldId { get; init; } = string.Empty;

        /// <summary>
        /// Human-readable question text from the CSV schema.
        /// </summary>
        public string Question { get; init; } = string.Empty;

        /// <summary>
        /// The type of validation to apply to this field's answer.
        /// </summary>
        public MarketplaceFieldType FieldType { get; init; }

        /// <summary>
        /// Maximum number of characters allowed. Null means no limit is enforced.
        /// </summary>
        public int? MaxLength { get; init; }

        /// <summary>
        /// Whether the field must be supplied. Fields left blank when required generate an error.
        /// </summary>
        public bool Required { get; init; }

        /// <summary>
        /// For <see cref="MarketplaceFieldType.SelectOne"/> and <see cref="MarketplaceFieldType.SelectOneOrMore"/>
        /// fields, the set of accepted values. Null or empty means any non-empty value is accepted.
        /// </summary>
        public IReadOnlyList<string>? AllowedValues { get; init; }
    }
}
