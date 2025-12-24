namespace PoC1_LegacyAnalyzer_Web.Models
{
    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Language { get; set; } = string.Empty; // Default should be set from configuration
        
        // Line count information
        public int LineCount { get; set; }
        public int NonEmptyLineCount { get; set; }
        public int CommentLineCount { get; set; }
        
        // C# specific metadata
        public List<string> UsingDirectives { get; set; } = new();
        public List<string> Namespaces { get; set; } = new();
        public List<string> ClassSignatures { get; set; } = new();
        public List<string> MethodSignatures { get; set; } = new();
        public List<string> PropertySignatures { get; set; } = new();
        
        // Language-agnostic metadata
        public List<string> Classes { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public List<string> Properties { get; set; } = new();
        public List<string> Interfaces { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        
        // Analysis results
        public ComplexityMetrics Complexity { get; set; } = new();
        public CodePatternAnalysis Patterns { get; set; } = new();
        public LegacyPatternResult? LegacyPatternResult { get; set; }
        public SemanticAnalysisResult? SemanticAnalysis { get; set; } // Hybrid semantic analysis for non-C# languages
        
        // Summary and status
        public string PatternSummary { get; set; } = string.Empty;
        public string CompactSummary { get; set; } = string.Empty;
        public string? OriginalCodeSnippet { get; set; }
        public string Status { get; set; } = string.Empty; // Default should be set from configuration
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

