namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Configuration for client-side encryption (key strategy, KDF parameters).
    /// </summary>
    public class ClientCryptoConfiguration
    {
        public string KeyStrategy { get; set; } = "GeneratedPerBrowser";
        public int KdfIterations { get; set; } = 300000;
        public int SaltLength { get; set; } = 16;
        public string AlgorithmName { get; set; } = "AES-GCM";
    }
}