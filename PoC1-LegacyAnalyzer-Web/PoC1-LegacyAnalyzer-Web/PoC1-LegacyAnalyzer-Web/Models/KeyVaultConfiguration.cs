namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Configuration for Azure Key Vault integration.
    /// </summary>
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// Azure Key Vault URI (e.g., https://your-keyvault.vault.azure.net/).
        /// </summary>
        public string? VaultUri { get; set; }

        /// <summary>
        /// Whether to use Azure Key Vault for configuration.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Managed Identity Client ID (optional, for user-assigned managed identity).
        /// </summary>
        public string? ManagedIdentityClientId { get; set; }

        /// <summary>
        /// Tenant ID for authentication (optional, defaults to Azure CLI or environment).
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Prefix for secret names in Key Vault (e.g., "App--").
        /// </summary>
        public string? SecretPrefix { get; set; }

        /// <summary>
        /// Reload interval in seconds for Key Vault secrets (0 = no reload).
        /// </summary>
        public int ReloadIntervalSeconds { get; set; } = 0;
    }
}

