namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Result of semantic analysis for non-C# languages.
    /// Combines Tree-sitter syntax analysis with AI-powered semantic analysis.
    /// </summary>
    public class SemanticAnalysisResult
    {
        /// <summary>
        /// Syntax analysis results from Tree-sitter (structure, classes, methods, etc.)
        /// </summary>
        public CodeAnalysisResult SyntaxAnalysis { get; set; } = new();

        /// <summary>
        /// Semantic issues detected by AI analysis.
        /// </summary>
        public List<SemanticIssue> SemanticIssues { get; set; } = new();

        /// <summary>
        /// Language-specific patterns detected.
        /// </summary>
        public List<LanguageSpecificPattern> DetectedPatterns { get; set; } = new();

        /// <summary>
        /// Overall semantic quality score (0-100).
        /// </summary>
        public int SemanticQualityScore { get; set; }
    }

    /// <summary>
    /// A semantic issue detected in the code (type errors, uninitialized variables, control flow issues, etc.)
    /// </summary>
    public class SemanticIssue
    {
        public string IssueType { get; set; } = string.Empty; // "TypeError", "UninitializedVariable", "ControlFlow", "DeprecatedPattern", etc.
        public string Category { get; set; } = string.Empty; // "Error", "Warning", "Info"
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int Severity { get; set; } // 1-5, higher is worse
    }

    /// <summary>
    /// Language-specific pattern detected (Python 2.x, var vs let/const, callback hell, etc.)
    /// </summary>
    public class LanguageSpecificPattern
    {
        public string PatternType { get; set; } = string.Empty; // "Python2Style", "VarUsage", "CallbackHell", "OldStyleClass", etc.
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = string.Empty;
        public string ModernAlternative { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool IsDeprecated { get; set; }
        public string MigrationGuidance { get; set; } = string.Empty;
    }
}

