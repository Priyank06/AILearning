namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    /// <summary>
    /// Results of pattern detection without AI
    /// </summary>
    public class CodePatternAnalysis
    {
        public List<string> DesignPatterns { get; set; } = new();
        public List<string> AntiPatterns { get; set; } = new();
        public List<CodeSmell> CodeSmells { get; set; } = new();

        // Security indicators
        public bool HasSqlConcatenation { get; set; }
        public bool HasHardcodedCredentials { get; set; }
        public bool HasDeprecatedCryptoAlgorithms { get; set; }

        // Performance indicators
        public bool HasSynchronousIO { get; set; }
        public bool HasPotentialMemoryLeaks { get; set; }
        public bool HasIneffcientLoops { get; set; }

        // Modernization indicators
        public bool UsesDeprecatedApis { get; set; }
        public bool MissingAsyncAwait { get; set; }
        public bool HasMagicNumbers { get; set; }
    }
}
