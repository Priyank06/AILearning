namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Encapsulates key/salt acquisition for client-side encryption without coupling callers to a specific approach.
    /// Key material never crosses to .NET; only opaque config/handles are exposed.
    /// </summary>
    public interface IEncryptionKeyStrategy
    {
        /// <summary>
        /// Gets or creates the crypto config (and ensures key is unlocked in JS). Returns opaque metadata only.
        /// </summary>
        Task<ClientCryptoConfig> GetOrCreateAsync(CancellationToken cancellationToken = default);
    }
}