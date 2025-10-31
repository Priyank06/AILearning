namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    /// <summary>
    /// Compact metadata extracted from a code file using static analysis
    /// This replaces sending full code to AI, reducing token usage by ~75%
    /// </summary>
    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int LineCount { get; set; }
        public int NonEmptyLineCount { get; set; }
        public int CommentLineCount { get; set; }

        // Structural elements
        public List<string> Classes { get; set; } = new();
        public List<string> Interfaces { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public List<string> Properties { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();

        // Analysis results
        public CodePatternAnalysis PatternAnalysis { get; set; } = new();
        public ComplexityMetrics Complexity { get; set; } = new();

        // Compact summary for AI (optimized for token efficiency)
        public string CompactSummary { get; set; } = string.Empty;
    }
}
