namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Opaque crypto configuration for client-side encryption. No key material crosses to .NET;
    /// this holds only algorithm metadata and an optional handle identifier for JS to use.
    /// </summary>
    public class ClientCryptoConfig
    {
        public int Version { get; set; } = 1;
        public string AlgorithmName { get; set; } = "AES-GCM";
        public string KdfName { get; set; } = "PBKDF2";
        /// <summary>Opaque identifier for JS to resolve the key in memory; never contains secret material.</summary>
        public string? KeyHandleId { get; set; }
    }
}