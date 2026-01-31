namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Thin wrapper over JS persistence/crypto APIs. Callers depend on this abstraction instead of IJSRuntime.
    /// Works with envelope-aware operations only; never handles raw key material.
    /// </summary>
    public interface ISecureClientInterop
    {
        /// <summary>Execute parameterized SQL on the decrypted in-memory DB (after load).</summary>
        Task ExecuteSqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>Query rows from the decrypted DB and map to T.</summary>
        Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>Serialize the in-memory DB to an encrypted blob and persist (CryptoEnvelope).</summary>
        Task SaveDatabaseAsync(CancellationToken cancellationToken = default);

        /// <summary>Load and decrypt the DB from storage into memory (using CryptoEnvelope).</summary>
        Task LoadDatabaseAsync(CancellationToken cancellationToken = default);

        /// <summary>Ensure the DB schema exists (create tables if needed).</summary>
        Task InitializeSchemaAsync(CancellationToken cancellationToken = default);

        /// <summary>Check if browser storage and crypto are available.</summary>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

        /// <summary>Export encrypted DB blob and envelope metadata (no plaintext key) for backup.</summary>
        Task<ExportBundle> ExportAsync(CancellationToken cancellationToken = default);

        /// <summary>Import from a previous export bundle.</summary>
        Task ImportAsync(ExportBundle bundle, CancellationToken cancellationToken = default);
    }

    /// <summary>Export bundle: db.enc payload, key envelope metadata (no plaintext key), and metadata.json contents.</summary>
    public class ExportBundle
    {
        public byte[] EncryptedDbPayload { get; set; } = Array.Empty<byte>();
        public string KeyEnvelopeJson { get; set; } = string.Empty;
        public string MetadataJson { get; set; } = string.Empty;
    }
}