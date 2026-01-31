namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Generic encrypted key-value/blob storage backed by JS interop. All data is encrypted with CryptoEnvelope before persisting.
    /// </summary>
    public interface ISecureClientStore
    {
        /// <summary>Store a value under key; encrypted before write.</summary>
        Task SetAsync(string key, byte[] value, CancellationToken cancellationToken = default);

        /// <summary>Retrieve and decrypt value for key; returns null if not found.</summary>
        Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Remove key.</summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Check if key exists.</summary>
        Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);
    }
}