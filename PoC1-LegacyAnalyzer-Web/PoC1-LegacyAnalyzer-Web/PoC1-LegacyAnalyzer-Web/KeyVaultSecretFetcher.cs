using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace PoC1_LegacyAnalyzer_Web
{
    /// <summary>
    /// Centralized service for secure secret retrieval from Azure Key Vault.
    /// </summary>
    public interface IKeyVaultService
    {
        Task<string?> GetSecretAsync(string secretName);
    }

    public class KeyVaultService : IKeyVaultService
    {
        private readonly SecretClient _client;
        private readonly ILogger _logger;

        public KeyVaultService(string vaultUri, ILogger<KeyVaultService> logger)
        {
            var options = new SecretClientOptions(SecretClientOptions.ServiceVersion.V7_2)
            {
                Retry = {
                    MaxRetries = 5,
                    Delay = TimeSpan.FromSeconds(2),
                    Mode = RetryMode.Exponential
                }
            };
            _client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential(), options);
            _logger = logger;
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            try
            {
                KeyVaultSecret secret = await _client.GetSecretAsync(secretName);
                _logger.LogInformation("Successfully fetched secret '{SecretName}' from Key Vault.", secretName);
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching secret '{SecretName}' from Key Vault.", secretName);
                return null;
            }
        }
    }
}
