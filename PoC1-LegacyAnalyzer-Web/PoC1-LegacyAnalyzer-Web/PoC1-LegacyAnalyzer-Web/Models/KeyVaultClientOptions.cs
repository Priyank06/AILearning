namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration options for the Azure Key Vault client retry behavior.
    /// </summary>
    public class KeyVaultClientOptions
    {
        /// <summary>
        /// Maximum number of retry attempts when calling Key Vault.
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// Delay in seconds between retry attempts.
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 2;
    }
}


