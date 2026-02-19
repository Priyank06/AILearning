namespace PoC1_LegacyAnalyzer_Web.Models.Marketplace
{
    /// <summary>
    /// Defines the type of validation expected for a marketplace CSV field.
    /// </summary>
    public enum MarketplaceFieldType
    {
        /// <summary>Accepts "True" or "False" only.</summary>
        TrueFalse,

        /// <summary>Accepts "True", "False", or "NA".</summary>
        TrueFalseNA,

        /// <summary>Must be a valid URL with a maximum of 500 characters.</summary>
        Url,

        /// <summary>Must be a valid email address.</summary>
        Email,

        /// <summary>Free text with a maximum of 500 characters.</summary>
        Text,

        /// <summary>Date value in yyyy/mm/dd format.</summary>
        Date,

        /// <summary>Must match exactly one of the allowed values.</summary>
        SelectOne,

        /// <summary>Must match one or more of the allowed values (comma-separated).</summary>
        SelectOneOrMore,

        /// <summary>Free text with no character limit enforced.</summary>
        FreeText,

        /// <summary>Section headers or non-data rows; skipped during validation.</summary>
        Ignored
    }
}
