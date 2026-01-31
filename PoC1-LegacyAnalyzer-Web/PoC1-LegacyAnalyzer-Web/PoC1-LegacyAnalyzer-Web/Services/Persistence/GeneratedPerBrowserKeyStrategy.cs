namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Key strategy that generates a key once per browser and persists a non-plaintext representation in IndexedDB.
    /// Key material never crosses to .NET; only opaque config is returned.
    /// </summary>
    public class GeneratedPerBrowserKeyStrategy : IEncryptionKeyStrategy
    {
        public Task<ClientCryptoConfig> GetOrCreateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ClientCryptoConfig
            {
                Version = 1,
                AlgorithmName = "AES-GCM",
                KdfName = "PBKDF2",
                KeyHandleId = "generated-per-browser"
            });
        }
    }
}