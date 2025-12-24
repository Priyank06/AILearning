namespace PoC1_LegacyAnalyzer_Web.Models
{
    /// <summary>
    /// Result of legacy pattern detection for a file or codebase.
    /// </summary>
    public class LegacyPatternResult
    {
        public List<LegacyIssue> Issues { get; set; } = new();
        public LegacyIndicators Indicators { get; set; } = new();
        public LegacyContext Context { get; set; } = new();
    }

    /// <summary>
    /// A specific legacy issue detected in the code.
    /// </summary>
    public class LegacyIssue
    {
        public string PatternType { get; set; } = ""; // GodObject, MagicNumber, CyclicDependency, DeadCode, ObsoleteApi, EmptyCatchBlock
        public string Severity { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string Description { get; set; } = "";
        public string Location { get; set; } = ""; // File name, class name, method name, line number
        public string Recommendation { get; set; } = "";
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = "";
    }

    /// <summary>
    /// Legacy indicators that provide context about the codebase age and framework.
    /// </summary>
    public class LegacyIndicators
    {
        public bool IsVeryOldCode { get; set; } // ⚠️ Very Old Code
        public bool IsAncientFramework { get; set; } // 🏛️ Ancient .NET Framework
        public bool HasGlobalState { get; set; } // 🌐 Global State Detected
        public bool HasLegacyDataAccess { get; set; }
        public bool UsesObsoleteApis { get; set; }
        public string? FrameworkVersion { get; set; }
        public int? EstimatedFileAgeYears { get; set; }
    }

    /// <summary>
    /// Context information about the file for legacy analysis.
    /// </summary>
    public class LegacyContext
    {
        public string? FileName { get; set; }
        public DateTime? FileLastModified { get; set; }
        public string? FrameworkVersion { get; set; }
        public int? ChangeFrequency { get; set; } // Number of changes in last year
        public int? LinesOfCode { get; set; }
        public string? Language { get; set; }
    }
}

