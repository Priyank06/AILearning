namespace PoC1_LegacyAnalyzer_Web.Models.MultiAgent
{
    public class Finding
    {
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Location { get; set; } = "";
        public List<string> Evidence { get; set; } = new();
    }

    public class FileMetadata
    {
        public string FileName { get; set; } = "";
        public string Language { get; set; } = "csharp";
        public long FileSize { get; set; }
        public int LineCount { get; set; }
        public int NonEmptyLineCount { get; set; }
        public int CommentLineCount { get; set; }
        public List<string> Classes { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public List<string> Properties { get; set; } = new();
        public List<string> Interfaces { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public ComplexityMetrics Complexity { get; set; } = new();
        public CodePatternAnalysis CodePatternAnalysis { get; set; } = new();
        // Compact AI Ready Summary
        public string CompactSummary { get; set; } = "";
        // Original Code Snippet
        public string? OriginalCodeSnippet { get; set; }
    }

    public class ProjectSummary
    {
        public int TotalFiles { get; set; }
        public int TotalLines { get; set; }
        public int TotalClasses { get; set; }
        public int TotalMethods { get; set; }
        public int TotalProperties { get; set; }
        public int TotalInterfaces { get; set; }
        public int TotalDependencies { get; set; }
        public int TotalComplexity { get; set; }
        public int TotalCodePatternAnalysis { get; set; }
        public string CompactSummary { get; set; } = "";
        // Language distribution
        public Dictionary<string, int> LanguageDistribution { get; set; } = new();

        // Pre-identified issues (found without AI)
        public List<CodeIssue> PreIdentifiedIssues { get; set; } = new();

        // Structured summary for AI (optimized format - ~500 tokens total)
        public string StructuredSummary { get; set; } = string.Empty;

        // File summaries (compact versions only)
        public List<FileMetadata> FileSummaries { get; set; } = new();
    }

    public class ComplexityMetrics
    {
        public int CyclomaticComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
        public int MaxNestingDepth { get; set; }
        public int NumberOfParameters { get; set; }
        public double MaintainabilityIndex { get; set; }
        public string ComplexityLevel { get; set; } = "Low"; // Low, Medium, High, VeryHigh
    }

    public class CodePatternAnalysis
    {
        // Security patterns
        public bool HasSqlInjectionRisk { get; set; }
        public bool HasHardcodedSecrets { get; set; }
        public bool HasWeakCryptography { get; set; }
        public bool HasPathTraversalRisk { get; set; }
        public bool HasCommandInjectionRisk { get; set; }

        // Legacy patterns
        public bool UsesDeprecatedApis { get; set; }
        public bool HasLegacyDataAccess { get; set; }
        public bool UsesOutdatedFrameworks { get; set; }

        // Code quality patterns
        public bool HasLongMethods { get; set; }
        public bool HasDeepNesting { get; set; }
        public bool HasMagicNumbers { get; set; }
        public bool HasEmptyCatchBlocks { get; set; }
        public bool HasTodoComments { get; set; }

        // Architecture patterns
        public bool UsesDesignPatterns { get; set; }
        public List<string> DetectedPatterns { get; set; } = new();
        public List<string> AntiPatterns { get; set; } = new();

        // Specific code smells with locations
        public List<CodeSmell> CodeSmells { get; set; } = new();
    }

    public class CodeSmell
    {
        public string SmellType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }
    
    public class CodeIssue
    {
        public string IssueType { get; set; } = string.Empty; // Security, Performance, Quality
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }
}
