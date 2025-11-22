using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using System;

namespace PoC1_LegacyAnalyzer_Web
{
    /// <summary>
    /// Key Vault secret manager that handles prefix-based secret naming.
    /// </summary>
    public class PrefixKeyVaultSecretManager : KeyVaultSecretManager
    {
        private readonly string _prefix;

        public PrefixKeyVaultSecretManager(string prefix)
        {
            _prefix = prefix ?? string.Empty;
        }

        public override bool Load(Azure.Security.KeyVault.Secrets.SecretProperties secret)
        {        
            return secret.Name.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase);
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            if (secret.Name.Length <= _prefix.Length)
                return string.Empty;
            var key = secret.Name.Substring(_prefix.Length);
            return key.Replace("--", ":");
        }
    }
}