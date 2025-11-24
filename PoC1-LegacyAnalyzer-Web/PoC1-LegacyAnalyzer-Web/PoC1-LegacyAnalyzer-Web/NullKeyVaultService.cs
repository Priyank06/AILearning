namespace PoC1_LegacyAnalyzer_Web
{
    /// <summary>
    /// Null implementation for local dev when Key Vault is disabled
    /// </summary>
    public class NullKeyVaultService : IKeyVaultService
    {
        private readonly ILogger _logger;

        public NullKeyVaultService(ILogger<NullKeyVaultService> logger)
        {
            _logger = logger;
        }

        public Task<string?> GetSecretAsync(string secretName)
        {
            _logger.LogDebug("NullKeyVaultService: Returning null for {SecretName}", secretName);
            return Task.FromResult<string?>(null);
        }
    }
}
